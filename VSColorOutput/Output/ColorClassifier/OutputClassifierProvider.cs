using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using VSColorOutput.State;

#pragma warning disable 649

namespace VSColorOutput.Output.ColorClassifier
{
    [ContentType("output")]
    [ContentType("plaintext")]
    [Export(typeof(IClassifierProvider))]
    public class OutputClassifierProvider : IClassifierProvider
    {
        private const  string           SuppliedClassifierForThisBufferKey = "VSColorOutput.Output.SuppliedClassifierForThisTextBufferKey";
        private const  string           BufferFilterStateKey = "VSColorOutput.Output.BufferLineFilterState";
        private static readonly object  BufferFilterStateLock = new object();

        [Import]
        internal IClassificationTypeRegistryService ClassificationRegistry;

        [Import]
        internal IClassificationFormatMapService ClassificationFormatMapService;

        private static OutputClassifier _outputClassifier;

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            try
            {
                if (CanSupplyClassifier(buffer))
                {
                    if (_outputClassifier == null)
                    {
                        Interlocked.CompareExchange(
                            ref _outputClassifier,
                            new OutputClassifier(),
                            null);

                        _outputClassifier.Initialize(ClassificationRegistry, ClassificationFormatMapService);
                    }

                    AttachHiddenLineFilter(buffer);
                    return _outputClassifier;
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.LogError(ex.ToString());
                throw;
            }
        }

        // This works around weird behavior of Visual Studio - when output window content
        // is saved to disk (using Ctrl+S) the ITextBuffer.ContentType is changed to "plaintext"
        // and it remains that way until Visual Studio is restarted.
        // The workaround tags each "output" so it can be identified after the content type is changed.
        private static bool CanSupplyClassifier(ITextBuffer buffer)
        {
            var bufferProperties = buffer.Properties;
            if (bufferProperties.ContainsProperty(SuppliedClassifierForThisBufferKey))
            {
                // We worked with this object before so we can work with it once again.
                return true;
            }

            var contentType = buffer.ContentType;
            if (contentType.IsOfType("Output"))
            {
                bufferProperties.AddProperty(SuppliedClassifierForThisBufferKey, null);
                return true;
            }

            return false;
        }

        private static void AttachHiddenLineFilter(ITextBuffer buffer)
        {
            if (buffer.Properties.ContainsProperty(BufferFilterStateKey)) return;

            EventHandler<TextContentChangedEventArgs> handler = null;
            handler = (sender, args) => HideMatchingLines(buffer, handler, args);

            lock (BufferFilterStateLock)
            {
                if (buffer.Properties.ContainsProperty(BufferFilterStateKey)) return;
                buffer.Properties.AddProperty(BufferFilterStateKey, handler);
                buffer.Changed += handler;
            }
        }

        private static void HideMatchingLines(ITextBuffer buffer, EventHandler<TextContentChangedEventArgs> handler, TextContentChangedEventArgs args)
        {
            if (args == null || args.After == null || args.EditTag == BufferFilterStateKey) return;

            try
            {
                var settings = Settings.Load();
                if (settings == null || string.IsNullOrWhiteSpace(settings.HiddenLinesRegExPattern)) return;

                var options = settings.HiddenLinesRegExIgnoreCase
                    ? RegexOptions.IgnoreCase
                    : RegexOptions.None;

                var regex = new Regex(
                    settings.HiddenLinesRegExPattern,
                    options,
                    TimeSpan.FromMilliseconds(250));

                var spansToDelete = new List<Span>();
                foreach (var change in args.Changes)
                {
                    var startLine = args.After.GetLineFromPosition(change.NewPosition).LineNumber;
                    var endPosition = change.NewEnd > change.NewPosition ? change.NewEnd - 1 : change.NewPosition;
                    var endLine = args.After.GetLineFromPosition(Math.Min(endPosition, Math.Max(args.After.Length - 1, 0))).LineNumber;

                    for (var i = startLine; i <= endLine; i++)
                    {
                        var line = args.After.GetLineFromLineNumber(i);
                        var text = line.GetText();
                        if (!regex.IsMatch(text)) continue;

                        var span = line.LineBreakLength > 0
                            ? line.ExtentIncludingLineBreak.Span
                            : line.Extent.Span;

                        spansToDelete.Add(span);
                    }
                }

                if (spansToDelete.Count == 0) return;

                using (var edit = buffer.CreateEdit(EditOptions.DefaultMinimalChange, null, BufferFilterStateKey))
                {
                    foreach (var span in spansToDelete
                                 .Distinct()
                                 .OrderByDescending(s => s.Start))
                    {
                        edit.Delete(span);
                    }

                    if (edit.HasEffectiveChanges)
                    {
                        edit.Apply();
                    }
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // eat it.
            }
            catch (ArgumentException)
            {
                // eat it.
            }
            catch (Exception ex)
            {
                Log.LogError(ex.ToString());
            }
        }
    }
}
