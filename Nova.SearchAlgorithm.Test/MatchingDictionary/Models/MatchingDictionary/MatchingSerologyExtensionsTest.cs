using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Models.MatchingDictionary
{
    [TestFixture]
    public class MatchingSerologyExtensionsTest
    {
        private const string SerologyLocus = "A";

        [TestCase("serology-name")]
        public void MatchingSerology_ToSerologyEntries_MapsSerologyName(string expectedSerologyName)
        {
            const SerologySubtype serologySubtype = SerologySubtype.NotSplit;
            const bool isDirectMapping = false;

            var serologyTyping = new SerologyTyping(SerologyLocus, expectedSerologyName, serologySubtype);
            var matchingSerology = new MatchingSerology(serologyTyping, isDirectMapping);
            var actual = matchingSerology.ToSerologyEntry();

            var expected = new SerologyEntry(expectedSerologyName, serologySubtype, isDirectMapping);

            actual.ShouldBeEquivalentTo(expected);
        }

        [TestCase(SerologySubtype.Associated)]
        [TestCase(SerologySubtype.Broad)]
        [TestCase(SerologySubtype.NotSplit)]
        [TestCase(SerologySubtype.Split)]
        public void MatchingSerology_ToSerologyEntries_MapsSerologySubtype(SerologySubtype expectedSerologySubtype)
        {
            const string serologyName = "serology-name";
            const bool isDirectMapping = false;

            var serologyTyping = new SerologyTyping(SerologyLocus, serologyName, expectedSerologySubtype);
            var matchingSerology = new MatchingSerology(serologyTyping, isDirectMapping);
            var actual = matchingSerology.ToSerologyEntry();

            var expected = new SerologyEntry(serologyName, expectedSerologySubtype, isDirectMapping);

            actual.ShouldBeEquivalentTo(expected);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MatchingSerology_ToSerologyEntries_MapsIsDirectMapping(bool expectedIsDirectMapping)
        {
            const string serologyName = "serology-name";
            const SerologySubtype serologySubtype = SerologySubtype.NotSplit;

            var serologyTyping = new SerologyTyping(SerologyLocus, serologyName, serologySubtype);
            var matchingSerology = new MatchingSerology(serologyTyping, expectedIsDirectMapping);
            var actual = matchingSerology.ToSerologyEntry();

            var expected = new SerologyEntry(serologyName, serologySubtype, expectedIsDirectMapping);

            actual.ShouldBeEquivalentTo(expected);
        }
    }
}
