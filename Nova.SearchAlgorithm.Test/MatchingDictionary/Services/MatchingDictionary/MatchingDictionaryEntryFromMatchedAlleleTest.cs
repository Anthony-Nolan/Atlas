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
    public class MatchingDictionaryEntryFromMatchedAlleleTest
    {
        private const MatchLocus Locus = MatchLocus.A;
        private static readonly string SerologyLocus = Locus.ToString();
        private static IEnumerable<string> _matchingPGroups;
        private static IEnumerable<string> _matchingGGroups;
        private static IList<RelDnaSerMapping> _matchingSerologies;
        private static IEnumerable<SerologyEntry> _matchingSerologyEntries;

        [OneTimeSetUp]
        public void SetUp()
        {
            _matchingPGroups = new List<string> { "01:01P" };
            _matchingGGroups = new List<string> { "01:01:01G" };

            var serology = new SerologyTyping(SerologyLocus, "1", SerologySubtype.NotSplit);
            _matchingSerologies = new List<RelDnaSerMapping>
            {
                new RelDnaSerMapping(serology, Assignment.None, new List<RelDnaSerMatch>{ new RelDnaSerMatch(serology) })
            };

            _matchingSerologyEntries = new List<SerologyEntry> { new SerologyEntry("1", SerologySubtype.NotSplit) };
        }

        private static IEnumerable<MatchingDictionaryEntry> GetActualMatchingDictionaryEntries(string alleleName)
        {
            var allele = new AlleleTyping($"{Locus}*", alleleName);

            var infoForMatching = Substitute.For<IAlleleInfoForMatching>();
            infoForMatching.HlaTyping.Returns(allele);
            infoForMatching.TypingUsedInMatching.Returns(allele);
            infoForMatching.MatchingPGroups.Returns(_matchingPGroups);
            infoForMatching.MatchingGGroups.Returns(_matchingGGroups);

            var matchedAllele = new List<MatchedAllele> {new MatchedAllele(infoForMatching, _matchingSerologies)};

            return matchedAllele.ToMatchingDictionaryEntries();
        }

        private static MatchingDictionaryEntry BuildExpectedMatchingDictionaryEntry(string lookupName, MolecularSubtype subtype)
        {
            return new MatchingDictionaryEntry(
                Locus, 
                lookupName, 
                TypingMethod.Molecular, 
                subtype, 
                SerologySubtype.NotSerologyTyping, 
                _matchingPGroups,
                _matchingGGroups,
                _matchingSerologyEntries);
        }

        [Test]
        public void MatchingDictionaryEntryCreatedFromAllele_FourFieldAllele()
        {
            var actual = GetActualMatchingDictionaryEntries("01:01:01:01");
            var expected = new List<MatchingDictionaryEntry>
            {
                BuildExpectedMatchingDictionaryEntry("01:01:01:01", MolecularSubtype.CompleteAllele),
                BuildExpectedMatchingDictionaryEntry("01:01", MolecularSubtype.TwoFieldAllele),
                BuildExpectedMatchingDictionaryEntry("01", MolecularSubtype.FirstFieldAllele)
            };

            Assert.IsTrue(actual.SequenceEqual(expected));
        }

        [Test]
        public void MatchingDictionaryEntryCreatedFromAllele_ThreeFieldAllele()
        {
            var actual = GetActualMatchingDictionaryEntries("01:01:02");
            var expected = new List<MatchingDictionaryEntry>
            {
                BuildExpectedMatchingDictionaryEntry("01:01:02", MolecularSubtype.CompleteAllele),
                BuildExpectedMatchingDictionaryEntry("01:01", MolecularSubtype.TwoFieldAllele),
                BuildExpectedMatchingDictionaryEntry("01", MolecularSubtype.FirstFieldAllele)
            };

            Assert.IsTrue(actual.SequenceEqual(expected));
        }

        [Test]
        public void MatchingDictionaryEntryCreatedFromAllele_TwoFieldAllele()
        {
            var actual = GetActualMatchingDictionaryEntries("01:32");
            var expected = new List<MatchingDictionaryEntry>
            {
                BuildExpectedMatchingDictionaryEntry("01:32", MolecularSubtype.CompleteAllele),
                BuildExpectedMatchingDictionaryEntry("01", MolecularSubtype.FirstFieldAllele)
            };

            Assert.IsTrue(actual.SequenceEqual(expected));
        }

        [Test]
        public void MatchingDictionaryEntryCreatedFromAllele_MoreThanTwoFieldExpressionLetter()
        {
            var actual = GetActualMatchingDictionaryEntries("01:01:38L");
            var expected = new List<MatchingDictionaryEntry>
            {
                BuildExpectedMatchingDictionaryEntry("01:01:38L", MolecularSubtype.CompleteAllele),
                BuildExpectedMatchingDictionaryEntry("01:01L", MolecularSubtype.TwoFieldAllele),
                BuildExpectedMatchingDictionaryEntry("01", MolecularSubtype.FirstFieldAllele)
            };

            Assert.IsTrue(actual.SequenceEqual(expected));
        }

        [Test]
        public void MatchingDictionaryEntryCreatedFromAllele_TwoFieldExpressionLetter()
        {
            var actual = GetActualMatchingDictionaryEntries("01:248Q");
            var expected = new List<MatchingDictionaryEntry>
            {
                BuildExpectedMatchingDictionaryEntry("01:248Q", MolecularSubtype.CompleteAllele),
                BuildExpectedMatchingDictionaryEntry("01", MolecularSubtype.FirstFieldAllele)
            };

            Assert.IsTrue(actual.SequenceEqual(expected));
        }
    }
}
