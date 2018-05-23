using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.MatchingDictionary
{
    [TestFixture]
    public class DictionaryFromMatchedAlleleTest
    {
        private const MatchLocus Locus = MatchLocus.A;
        private static readonly string SerologyLocus = Locus.ToString();
        private static IEnumerable<string> _matchingPGroups;
        private static IList<RelDnaSerMapping> _matchingSerologies;
        private static IEnumerable<SerologyEntry> _matchingSerologyEntries;

        [OneTimeSetUp]
        public void SetUp()
        {
            _matchingPGroups = new List<string> { "01:01P" };

            var serology = new SerologyTyping(SerologyLocus, "1", SerologySubtype.NotSplit);
            _matchingSerologies = new List<RelDnaSerMapping>
            {
                new RelDnaSerMapping(serology, Assignment.None, new List<RelDnaSerMatch>{ new RelDnaSerMatch(serology) })
            };

            _matchingSerologyEntries = new List<SerologyEntry> { new SerologyEntry("1", SerologySubtype.NotSplit) };
        }

        private static IEnumerable<MatchingDictionaryEntry> GetActualDictionaryEntries(string alleleName)
        {
            var allele = new AlleleTyping($"{Locus}*", alleleName);

            var alleleToPGroup = Substitute.For<IAlleleInfoForMatching>();
            alleleToPGroup.HlaTyping.Returns(allele);
            alleleToPGroup.TypingUsedInMatching.Returns(allele);
            alleleToPGroup.MatchingPGroups.Returns(_matchingPGroups);

            var matchedAllele = new List<MatchedAllele> {new MatchedAllele(alleleToPGroup, _matchingSerologies)};

            return matchedAllele.ToMatchingDictionaryEntries();
        }

        private static MatchingDictionaryEntry BuildExpectedDictionaryEntry(string lookupName, MolecularSubtype subtype)
        {
            return new MatchingDictionaryEntry(
                Locus, lookupName, TypingMethod.Molecular, subtype, SerologySubtype.NotSerologyTyping, _matchingPGroups, _matchingSerologyEntries);
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
