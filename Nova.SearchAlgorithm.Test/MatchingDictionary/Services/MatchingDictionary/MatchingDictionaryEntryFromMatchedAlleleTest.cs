using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
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
    public class MatchedAlleleToMatchingDictionaryEntryTest
    {
        [TestCase("01:01:02", "")]
        [TestCase("01:01:01:01", "")]
        [TestCase("01:01:38L", "L")]
        [TestCase("01:01:01:02N", "N")]
        public void MatchedAlleleToMatchingDictionaryEntry_ThreeOrFourFieldAllele_ThreeEntriesGenerated(string alleleName, string expressionSuffix)
        {
            var matchedAllele = BuildTestObjectFromAlleleName(alleleName);

            var expected = new List<MatchingDictionaryEntry>
            {
                new MatchingDictionaryEntry(matchedAllele, alleleName, MolecularSubtype.CompleteAllele),
                new MatchingDictionaryEntry(matchedAllele, "01:01" + expressionSuffix, MolecularSubtype.TwoFieldAllele),
                new MatchingDictionaryEntry(matchedAllele, "01", MolecularSubtype.FirstFieldAllele)
            };

            TestGenerationOfMatchingDictionaryEntriesFromMatchedAlleles(new[] { matchedAllele }, expected);
        }

        [TestCase("01:32", "")]
        [TestCase("01:248Q", "Q")]
        public void MatchedAlleleToMatchingDictionaryEntry_TwoFieldAllele_TwoEntriesGenerated(string alleleName, string expressionSuffix)
        {
            var matchedAllele = BuildTestObjectFromAlleleName(alleleName);

            var expected = new List<MatchingDictionaryEntry>
            {
                new MatchingDictionaryEntry(matchedAllele, alleleName, MolecularSubtype.CompleteAllele),
                new MatchingDictionaryEntry(matchedAllele, "01", MolecularSubtype.FirstFieldAllele)
            };

            TestGenerationOfMatchingDictionaryEntriesFromMatchedAlleles(new[] { matchedAllele }, expected);
        }

        [Test]
        public void MatchedAlleleToMatchingDictionaryEntry_AllelesWithSameTruncatedNames_OneEntryPerNameAndSubtypeCombination()
        {
            string[] alleles = { "01:01:01:01", "01:01:01:03", "01:01:51" };

            var matchedAlleles = alleles.Select(BuildTestObjectFromAlleleName).ToList();

            var expected = new List<MatchingDictionaryEntry>
            {
                new MatchingDictionaryEntry(matchedAlleles[0], alleles[0], MolecularSubtype.CompleteAllele),
                new MatchingDictionaryEntry(matchedAlleles[1], alleles[1], MolecularSubtype.CompleteAllele),
                new MatchingDictionaryEntry(matchedAlleles[2], alleles[2], MolecularSubtype.CompleteAllele),
                new MatchingDictionaryEntry(matchedAlleles[0], "01:01", MolecularSubtype.TwoFieldAllele),
                new MatchingDictionaryEntry(matchedAlleles[0], "01", MolecularSubtype.FirstFieldAllele)
            };

            TestGenerationOfMatchingDictionaryEntriesFromMatchedAlleles(matchedAlleles, expected);
        }

        private static MatchedAllele BuildTestObjectFromAlleleName(string alleleName)
        {
            const string locus = "A*";

            var infoForMatching = Substitute.For<IAlleleInfoForMatching>();
            infoForMatching.HlaTyping.Returns(new AlleleTyping(locus, alleleName));

            return new MatchedAllele(infoForMatching, new List<SerologyMappingForAllele>());
        }

        private static void TestGenerationOfMatchingDictionaryEntriesFromMatchedAlleles(
            IEnumerable<MatchedAllele> matchedAlleles, IEnumerable<MatchingDictionaryEntry> expected)
        {
            var actual = matchedAlleles.ToMatchingDictionaryEntries();
            actual.Should().BeEquivalentTo(expected);
        }
    }
}
