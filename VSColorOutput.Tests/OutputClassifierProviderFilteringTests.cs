using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Moq;
using System;
using System.Reflection;
using VSColorOutput.Output.ColorClassifier;

namespace Tests
{
    [TestClass]
    public class OutputClassifierProviderFilteringTests
    {
        private const string BufferFilterStateKey = "VSColorOutput.Output.BufferLineFilterState";

        [TestInitialize]
        public void Setup()
        {
            var field = typeof(OutputClassifierProvider).GetField("_outputClassifier", BindingFlags.Static | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(null, null);
            }
        }

        [TestMethod]
        public void GetClassifier_AttachesHiddenLineFilterStateToOutputBuffer()
        {
            var provider = CreateProvider();

            var properties = new PropertyCollection();
            var contentType = new Mock<IContentType>();
            contentType.Setup(c => c.IsOfType("Output")).Returns(true);

            var buffer = new Mock<ITextBuffer>();
            buffer.SetupGet(b => b.Properties).Returns(properties);
            buffer.SetupGet(b => b.ContentType).Returns(contentType.Object);

            var classifier = provider.GetClassifier(buffer.Object);

            classifier.Should().NotBeNull();
            properties.ContainsProperty(BufferFilterStateKey).Should().BeTrue();
        }

        [TestMethod]
        public void GetClassifier_AttachesHiddenLineFilterOnlyOncePerBuffer()
        {
            var provider = CreateProvider();

            var properties = new PropertyCollection();
            var contentType = new Mock<IContentType>();
            contentType.Setup(c => c.IsOfType("Output")).Returns(true);

            var changedSubscriptionCount = 0;
            var buffer = new Mock<ITextBuffer>();
            buffer.SetupGet(b => b.Properties).Returns(properties);
            buffer.SetupGet(b => b.ContentType).Returns(contentType.Object);
            buffer.SetupAdd(b => b.Changed += It.IsAny<EventHandler<TextContentChangedEventArgs>>())
                  .Callback(() => changedSubscriptionCount++);

            provider.GetClassifier(buffer.Object);
            provider.GetClassifier(buffer.Object);

            changedSubscriptionCount.Should().Be(1);
        }

        private static OutputClassifierProvider CreateProvider()
        {
            var provider = new OutputClassifierProvider();

            var registryField = typeof(OutputClassifierProvider).GetField("ClassificationRegistry", BindingFlags.Instance | BindingFlags.NonPublic);
            registryField.SetValue(provider, new Mock<IClassificationTypeRegistryService>().Object);

            var formatMapField = typeof(OutputClassifierProvider).GetField("ClassificationFormatMapService", BindingFlags.Instance | BindingFlags.NonPublic);
            formatMapField.SetValue(provider, new Mock<IClassificationFormatMapService>().Object);

            return provider;
        }
    }
}
