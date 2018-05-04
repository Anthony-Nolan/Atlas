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
    public class DictionaryFromAlleleTest
    {
        private DictionaryFromAllele dictionaryFromAllele;
        private const string Locus = "A";
        private static IEnumerable<string> _matchingPGroups;
        private static IEnumerable<Serology> _matchingSerologies;
        private static IEnumerable<SerologyEntry> _matchingSerologyEntries;

        [SetUp]
        public void SetUp()
        {
            dictionaryFromAllele = new DictionaryFromAllele();
            _matchingPGroups = new List<string> { "01:01P" };
            _matchingSerologies = new List<Serology> {new Serology(Locus, "1", SerologySubtype.NotSplit)};
            _matchingSerologyEntries = new List<SerologyEntry>{ new SerologyEntry("1", SerologySubtype.NotSplit)};
        }

        private static IDictionaryAlleleSource BuildAlleleSource(string alleleName)
        {
            var alleleSource = Substitute.For<IDictionaryAlleleSource>();
            alleleSource.MatchedOnAllele.Returns(new Allele($"{Locus}*", alleleName));
            alleleSource.MatchingPGroups.Returns(_matchingPGroups);
            alleleSource.MatchingSerologies.Returns(_matchingSerologies);
            return alleleSource;
        }

        private static MatchingDictionaryEntry GetExpectedDictionaryEntry(string lookupName, MolecularSubtype subtype)
        {
            return new MatchingDictionaryEntry(
                Locus, lookupName, TypingMethod.Molecular, subtype, SerologySubtype.NotSerologyType, _matchingPGroups, _matchingSerologyEntries);
        }

        [Test]
        public void DictionaryEntryCreatedFromAlleleSource_FourFieldAllele()
        {
            var alleleSource = BuildAlleleSource("01:01:01:01");
            var actual = dictionaryFromAllele.GetDictionaryEntries(new List<IDictionaryAlleleSource> { alleleSource });
            var expected = new List<MatchingDictionaryEntry>
            {
                GetExpectedDictionaryEntry("01:01:01:01", MolecularSubtype.CompleteAllele),
                GetExpectedDictionaryEntry("01:01", MolecularSubtype.TwoFieldAllele),
                GetExpectedDictionaryEntry("01", MolecularSubtype.FirstFieldAllele)
            };

            Assert.IsTrue(actual.SequenceEqual(expected));
        }

        [Test]
        public void DictionaryEntryCreatedFromAlleleSource_ThreeFieldAllele()
        {
            var alleleSource = BuildAlleleSource("01:01:02");
            var actual = dictionaryFromAllele.GetDictionaryEntries(new List<IDictionaryAlleleSource> { alleleSource });
            var expected = new List<MatchingDictionaryEntry>
            {
                GetExpectedDictionaryEntry("01:01:02", MolecularSubtype.CompleteAllele),
                GetExpectedDictionaryEntry("01:01", MolecularSubtype.TwoFieldAllele),
                GetExpectedDictionaryEntry("01", MolecularSubtype.FirstFieldAllele)
            };

            Assert.IsTrue(actual.SequenceEqual(expected));
        }

        [Test]
        public void DictionaryEntryCreatedFromAlleleSource_TwoFieldAllele()
        {
            var alleleSource = BuildAlleleSource("01:32");
            var actual = dictionaryFromAllele.GetDictionaryEntries(new List<IDictionaryAlleleSource> { alleleSource });
            var expected = new List<MatchingDictionaryEntry>
            {
                GetExpectedDictionaryEntry("01:32", MolecularSubtype.CompleteAllele),
                GetExpectedDictionaryEntry("01", MolecularSubtype.FirstFieldAllele)
            };

            Assert.IsTrue(actual.SequenceEqual(expected));
        }

        [Test]
        public void DictionaryEntryCreatedFromAlleleSource_MoreThanTwoFieldExpressionLetter()
        {
            var alleleSource = BuildAlleleSource("01:01:38L");
            var actual = dictionaryFromAllele.GetDictionaryEntries(new List<IDictionaryAlleleSource> { alleleSource });
            var expected = new List<MatchingDictionaryEntry>
            {
                GetExpectedDictionaryEntry("01:01:38L", MolecularSubtype.CompleteAllele),
                GetExpectedDictionaryEntry("01:01L", MolecularSubtype.TwoFieldAllele),
                GetExpectedDictionaryEntry("01", MolecularSubtype.FirstFieldAllele)
            };

            Assert.IsTrue(actual.SequenceEqual(expected));
        }

        [Test]
        public void DictionaryEntryCreatedFromAlleleSource_TwoFieldExpressionLetter()
        {
            var alleleSource = BuildAlleleSource("01:248Q");
            var actual = dictionaryFromAllele.GetDictionaryEntries(new List<IDictionaryAlleleSource> { alleleSource });
            var expected = new List<MatchingDictionaryEntry>
            {
                GetExpectedDictionaryEntry("01:248Q", MolecularSubtype.CompleteAllele),
                GetExpectedDictionaryEntry("01", MolecularSubtype.FirstFieldAllele)
            };

            Assert.IsTrue(actual.SequenceEqual(expected));
        }
    }
}
