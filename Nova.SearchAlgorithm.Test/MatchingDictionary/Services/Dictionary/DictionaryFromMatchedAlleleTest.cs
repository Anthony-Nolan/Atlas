using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Dictionary
{
    [TestFixture]
    public class DictionaryFromMatchedAlleleTest
    {
        private const string Locus = "A";
        private static IEnumerable<string> _matchingPGroups;
        private static IList<SerologyMappingInfo> _matchingSerologies;
        private static IEnumerable<SerologyEntry> _matchingSerologyEntries;

        [SetUp]
        public void SetUp()
        {
            _matchingPGroups = new List<string> { "01:01P" };

            var serology = new Serology(Locus, "1", SerologySubtype.NotSplit);
            _matchingSerologies = new List<SerologyMappingInfo>
            {
                new SerologyMappingInfo(serology, Assignment.None, new List<SerologyMatchInfo>{ new SerologyMatchInfo(serology) })
            };

            _matchingSerologyEntries = new List<SerologyEntry> { new SerologyEntry("1", SerologySubtype.NotSplit) };
        }

        private static IEnumerable<MatchingDictionaryEntry> GetActualDictionaryEntries(string alleleName)
        {
            var allele = new Allele($"{Locus}*", alleleName);

            var alleleToPGroup = Substitute.For<IAlleleToPGroup>();
            alleleToPGroup.HlaType.Returns(allele);
            alleleToPGroup.TypeUsedInMatching.Returns(allele);
            alleleToPGroup.MatchingPGroups.Returns(_matchingPGroups);

            var matchedAllele = new List<MatchedAllele> {new MatchedAllele(alleleToPGroup, _matchingSerologies)};

            return matchedAllele.ToMatchingDictionaryEntries();
        }

        private static MatchingDictionaryEntry BuildExpectedDictionaryEntry(string lookupName, MolecularSubtype subtype)
        {
            return new MatchingDictionaryEntry(
                Locus, lookupName, TypingMethod.Molecular, subtype, SerologySubtype.NotSerologyType, _matchingPGroups, _matchingSerologyEntries);
        }

        [Test]
        public void DictionaryEntryCreatedFromAlleleSource_FourFieldAllele()
        {
            var actual = GetActualDictionaryEntries("01:01:01:01");
            var expected = new List<MatchingDictionaryEntry>
            {
                BuildExpectedDictionaryEntry("01:01:01:01", MolecularSubtype.CompleteAllele),
                BuildExpectedDictionaryEntry("01:01", MolecularSubtype.TwoFieldAllele),
                BuildExpectedDictionaryEntry("01", MolecularSubtype.FirstFieldAllele)
            };

            Assert.IsTrue(actual.SequenceEqual(expected));
        }

        [Test]
        public void DictionaryEntryCreatedFromAlleleSource_ThreeFieldAllele()
        {
            var actual = GetActualDictionaryEntries("01:01:02");
            var expected = new List<MatchingDictionaryEntry>
            {
                BuildExpectedDictionaryEntry("01:01:02", MolecularSubtype.CompleteAllele),
                BuildExpectedDictionaryEntry("01:01", MolecularSubtype.TwoFieldAllele),
                BuildExpectedDictionaryEntry("01", MolecularSubtype.FirstFieldAllele)
            };

            Assert.IsTrue(actual.SequenceEqual(expected));
        }

        [Test]
        public void DictionaryEntryCreatedFromAlleleSource_TwoFieldAllele()
        {
            var actual = GetActualDictionaryEntries("01:32");
            var expected = new List<MatchingDictionaryEntry>
            {
                BuildExpectedDictionaryEntry("01:32", MolecularSubtype.CompleteAllele),
                BuildExpectedDictionaryEntry("01", MolecularSubtype.FirstFieldAllele)
            };

            Assert.IsTrue(actual.SequenceEqual(expected));
        }

        [Test]
        public void DictionaryEntryCreatedFromAlleleSource_MoreThanTwoFieldExpressionLetter()
        {
            var actual = GetActualDictionaryEntries("01:01:38L");
            var expected = new List<MatchingDictionaryEntry>
            {
                BuildExpectedDictionaryEntry("01:01:38L", MolecularSubtype.CompleteAllele),
                BuildExpectedDictionaryEntry("01:01L", MolecularSubtype.TwoFieldAllele),
                BuildExpectedDictionaryEntry("01", MolecularSubtype.FirstFieldAllele)
            };

            Assert.IsTrue(actual.SequenceEqual(expected));
        }

        [Test]
        public void DictionaryEntryCreatedFromAlleleSource_TwoFieldExpressionLetter()
        {
            var actual = GetActualDictionaryEntries("01:248Q");
            var expected = new List<MatchingDictionaryEntry>
            {
                BuildExpectedDictionaryEntry("01:248Q", MolecularSubtype.CompleteAllele),
                BuildExpectedDictionaryEntry("01", MolecularSubtype.FirstFieldAllele)
            };

            Assert.IsTrue(actual.SequenceEqual(expected));
        }
    }
}
