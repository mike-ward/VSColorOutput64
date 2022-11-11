using Microsoft.VisualStudio.Text.Classification;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
    public class FakeClassificationType : IClassificationType
    {
        public FakeClassificationType(string classification) => Classification = classification;

        public bool IsOfType(string type)
        {
            return false;
        }

        public string Classification { get; }

        public IEnumerable<IClassificationType> BaseTypes => Enumerable.Empty<IClassificationType>();
    }
}