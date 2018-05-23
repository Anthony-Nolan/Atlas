using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.MatchingDictionary
{
    [TestFixture]
    public class DictionaryFromMatchedSerologyTest
    {
        private const MatchLocus Locus = MatchLocus.A;
        private static readonly string SerologyLocus = Locus.ToString();
        private static IEnumerable<string> _matchingPGroups;
        private static IEnumerable<SerologyTyping> _matchingSerologies;
        private static IEnumerable<SerologyEntry> _matchingSerologyEntries;

        [OneTimeSetUp]
        public void SetUp()
        {
            _matchingPGroups = new List<string> { "123:123P" };
            _matchingSerologies = new List<SerologyTyping> {new SerologyTyping(SerologyLocus, "123", SerologySubtype.NotSplit)};
            _matchingSerologyEntries = new List<SerologyEntry>{ new SerologyEntry("123", SerologySubtype.NotSplit)};
        }

        private static MatchedSerology BuildMatchedSerology(string serologyName, SerologySubtype subtype)
        {
            var serology = new SerologyTyping(SerologyLocus, serologyName, subtype);

            var serologyToSerology = Substitute.For<ISerologyInfoForMatching>();
            serologyToSerology.HlaTyping.Returns(serology);
            serologyToSerology.TypingUsedInMatching.Returns(serology);
            serologyToSerology.MatchingSerologies.Returns(_matchingSerologies);

            return new MatchedSerology(serologyToSerology, _matchingPGroups);
        }

        private static MatchingDictionaryEntry GetExpectedDictionaryEntry(string lookupName, SerologySubtype subtype)
        {
            return new MatchingDictionaryEntry(
                Locus, lookupName, TypingMethod.Serology, MolecularSubtype.NotMolecularTyping, subtype, _matchingPGroups, _matchingSerologyEntries);
        }

        [Test]
        public void DictionaryEntriesCreatedFromSerology()
        {
            var serologySource = new List<MatchedSerology>
            {
                BuildMatchedSerology("9", SerologySubtype.Broad),
                BuildMatchedSerology("23", SerologySubtype.Split),
                BuildMatchedSerology("2403", SerologySubtype.Associated),
                BuildMatchedSerology("1", SerologySubtype.NotSplit)
            };
            var actual = serologySource.ToMatchingDictionaryEntries();

            var expected = new List<MatchingDictionaryEntry>
            {
                GetExpectedDictionaryEntry("9", SerologySubtype.Broad),
                GetExpectedDictionaryEntry("23", SerologySubtype.Split),
                GetExpectedDictionaryEntry("2403", SerologySubtype.Associated),
                GetExpectedDictionaryEntry("1", SerologySubtype.NotSplit)
            };

            Assert.IsTrue(actual.SequenceEqual(expected));
        }
    }
}
