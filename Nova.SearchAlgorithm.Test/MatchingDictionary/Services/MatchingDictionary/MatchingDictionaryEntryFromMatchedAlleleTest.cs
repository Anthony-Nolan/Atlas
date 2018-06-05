using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.MatchingDictionary
{
    [TestFixture]
    public class MatchingDictionaryEntryFromMatchedAlleleTest
    {
        private const MatchLocus Locus = MatchLocus.A;
        private static readonly string SerologyLocus = Locus.ToString();
        private static IEnumerable<string> _matchingPGroups;
        private static IEnumerable<string> _matchingGGroups;
        private static IList<SerologyMappingForAllele> _matchingSerologies;

        [OneTimeSetUp]
        public void SetUp()
        {
            _matchingPGroups = new List<string> { "01:01P" };
            _matchingGGroups = new List<string> { "01:01:01G" };

            var serology = new SerologyTyping(SerologyLocus, "1", SerologySubtype.NotSplit);
            _matchingSerologies = new List<SerologyMappingForAllele>
            {
                new SerologyMappingForAllele(serology, Assignment.None, new List<SerologyMatch>{ new SerologyMatch(serology) })
            };
        }

        private static IEnumerable<MatchedAllele> BuildMatchedAlleles(string alleleName)
        {
            var allele = new AlleleTyping($"{Locus}*", alleleName);

            var infoForMatching = Substitute.For<IAlleleInfoForMatching>();
            infoForMatching.HlaTyping.Returns(allele);
            infoForMatching.TypingUsedInMatching.Returns(allele);
            infoForMatching.MatchingPGroups.Returns(_matchingPGroups);
            infoForMatching.MatchingGGroups.Returns(_matchingGGroups);

            return new List<MatchedAllele> { new MatchedAllele(infoForMatching, _matchingSerologies) };
        }

        [Test]
        public void MatchingDictionaryEntryCreatedFromAllele_FourFieldAllele()
        {
            const string originalAlleleName = "01:01:01:01";

            var matchedAlleles = BuildMatchedAlleles(originalAlleleName).ToList();

            var expected = new List<MatchingDictionaryEntry>
            {
                new MatchingDictionaryEntry(matchedAlleles.First(), originalAlleleName, MolecularSubtype.CompleteAllele),
                new MatchingDictionaryEntry(matchedAlleles.First(), "01:01", MolecularSubtype.TwoFieldAllele),
                new MatchingDictionaryEntry(matchedAlleles.First(), "01", MolecularSubtype.FirstFieldAllele)
            };

            AssertThatActualResultsEqualExpected(matchedAlleles, expected);
        }

        [Test]
        public void MatchingDictionaryEntryCreatedFromAllele_ThreeFieldAllele()
        {
            const string originalAlleleName = "01:01:02";

            var matchedAlleles = BuildMatchedAlleles(originalAlleleName).ToList();

            var expected = new List<MatchingDictionaryEntry>
            {
                new MatchingDictionaryEntry(matchedAlleles.First(), originalAlleleName, MolecularSubtype.CompleteAllele),
                new MatchingDictionaryEntry(matchedAlleles.First(), "01:01", MolecularSubtype.TwoFieldAllele),
                new MatchingDictionaryEntry(matchedAlleles.First(), "01", MolecularSubtype.FirstFieldAllele)
            };

            AssertThatActualResultsEqualExpected(matchedAlleles, expected);
        }

        [Test]
        public void MatchingDictionaryEntryCreatedFromAllele_TwoFieldAllele()
        {
            const string originalAlleleName = "01:32";

            var matchedAlleles = BuildMatchedAlleles(originalAlleleName).ToList();

            var expected = new List<MatchingDictionaryEntry>
            {
                new MatchingDictionaryEntry(matchedAlleles.First(), originalAlleleName, MolecularSubtype.CompleteAllele),
                new MatchingDictionaryEntry(matchedAlleles.First(), "01", MolecularSubtype.FirstFieldAllele)
            };

            AssertThatActualResultsEqualExpected(matchedAlleles, expected);
        }

        [Test]
        public void MatchingDictionaryEntryCreatedFromAllele_MoreThanTwoFieldExpressionLetter()
        {
            const string originalAlleleName = "01:01:38L";

            var matchedAlleles = BuildMatchedAlleles(originalAlleleName).ToList();

            var expected = new List<MatchingDictionaryEntry>
            {
                new MatchingDictionaryEntry(matchedAlleles.First(), originalAlleleName, MolecularSubtype.CompleteAllele),
                new MatchingDictionaryEntry(matchedAlleles.First(), "01:01L", MolecularSubtype.TwoFieldAllele),
                new MatchingDictionaryEntry(matchedAlleles.First(), "01", MolecularSubtype.FirstFieldAllele)
            };

            AssertThatActualResultsEqualExpected(matchedAlleles, expected);
        }

        [Test]
        public void MatchingDictionaryEntryCreatedFromAllele_TwoFieldExpressionLetter()
        {
            const string originalAlleleName = "01:248Q";

            var matchedAlleles = BuildMatchedAlleles(originalAlleleName).ToList();

            var expected = new List<MatchingDictionaryEntry>
            {
                new MatchingDictionaryEntry(matchedAlleles.First(), originalAlleleName, MolecularSubtype.CompleteAllele),
                new MatchingDictionaryEntry(matchedAlleles.First(), "01", MolecularSubtype.FirstFieldAllele)
            };

            AssertThatActualResultsEqualExpected(matchedAlleles, expected);
        }

        [Test]
        public void MatchingDictionaryEntryCreatedFromAllele_MultipleAllelesWithSameTruncatedNames()
        {
            string[] alleles = { "01:01:01:01", "01:01:01:03", "01:01:51" };

            var matchedAlleles = alleles.SelectMany(BuildMatchedAlleles).ToList();

            var expected = new List<MatchingDictionaryEntry>
            {
                new MatchingDictionaryEntry(matchedAlleles[0], alleles[0], MolecularSubtype.CompleteAllele),
                new MatchingDictionaryEntry(matchedAlleles.First(), "01:01", MolecularSubtype.TwoFieldAllele),
                new MatchingDictionaryEntry(matchedAlleles.First(), "01", MolecularSubtype.FirstFieldAllele),
                new MatchingDictionaryEntry(matchedAlleles[1], alleles[1], MolecularSubtype.CompleteAllele),
                new MatchingDictionaryEntry(matchedAlleles[2], alleles[2], MolecularSubtype.CompleteAllele)
            };

            AssertThatActualResultsEqualExpected(matchedAlleles, expected);
        }

        private static void AssertThatActualResultsEqualExpected(IEnumerable<MatchedAllele> matchedAlleles, IEnumerable<MatchingDictionaryEntry> expected)
        {
            var actual = matchedAlleles.ToMatchingDictionaryEntries();
            actual.Should().BeEquivalentTo(expected);
        }
    }
}
