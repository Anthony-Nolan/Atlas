using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Dictionary
{
    [TestFixture]
    public class DictionaryFromSerologyTest
    {
        private DictionaryFromSerology dictionaryFromSerology;
        private const string Locus = "A";
        private static IEnumerable<string> _matchingPGroups;
        private static IEnumerable<Serology> _matchingSerologies;
        private static IEnumerable<SerologyEntry> _matchingSerologyEntries;

        [SetUp]
        public void SetUp()
        {
            dictionaryFromSerology = new DictionaryFromSerology();
            _matchingPGroups = new List<string> { "123:123P" };
            _matchingSerologies = new List<Serology> {new Serology(Locus, "123", SerologySubtype.NotSplit)};
            _matchingSerologyEntries = new List<SerologyEntry>{ new SerologyEntry("123", SerologySubtype.NotSplit)};
        }

        private static IDictionarySerologySource BuildSerologySource(string serologyName, SerologySubtype subtype)
        {
            var serologySource = Substitute.For<IDictionarySerologySource>();
            serologySource.MatchedOnSerology.Returns(new Serology(Locus, serologyName, subtype));
            serologySource.MatchingPGroups.Returns(_matchingPGroups);
            serologySource.MatchingSerologies.Returns(_matchingSerologies);
            return serologySource;
        }

        private static MatchingDictionaryEntry GetExpectedDictionaryEntry(string lookupName, SerologySubtype subtype)
        {
            return new MatchingDictionaryEntry(
                Locus, lookupName, TypingMethod.Serology, MolecularSubtype.NotMolecularType, subtype, _matchingPGroups, _matchingSerologyEntries);
        }

        [Test]
        public void DictionaryEntriesCreatedFromSerologySource()
        {
            var serologySource = new List<IDictionarySerologySource>
            {
                BuildSerologySource("9", SerologySubtype.Broad),
                BuildSerologySource("23", SerologySubtype.Split),
                BuildSerologySource("2403", SerologySubtype.Associated),
                BuildSerologySource("1", SerologySubtype.NotSplit)
            };
            var actual = dictionaryFromSerology.GetDictionaryEntries(serologySource);

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
