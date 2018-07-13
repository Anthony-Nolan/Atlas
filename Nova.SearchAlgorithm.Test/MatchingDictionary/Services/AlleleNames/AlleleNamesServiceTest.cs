using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.AlleleNames
{
    public class AlleleNamesServiceTest
    {
        private List<AlleleNameLookupResult> alleleNameLookupResults;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var dataRepository = SharedTestDataCache.GetWmdaDataRepository();
            var historiesConsolidator = new AlleleNameHistoriesConsolidator(dataRepository);
            var fromExtractorExtractor = new AlleleNamesFromHistoriesExtractor(historiesConsolidator, dataRepository);
            var variantsExtractor = new AlleleNameVariantsExtractor(dataRepository);
            var reservedNamesExtractor = new ReservedAlleleNamesExtractor(dataRepository);
            var alleleNamesRepository = Substitute.For<IAlleleNamesLookupRepository>();

            alleleNameLookupResults = new AlleleNamesService(
                    fromExtractorExtractor, variantsExtractor, reservedNamesExtractor, alleleNamesRepository)
                .GetAlleleNamesAndTheirVariants()
                .ToList();
        }

        [Test]
        public void AlleleNamesService_GetAlleleNamesAndTheirVariants_DoesNotGenerateDuplicateAlleleNames()
        {
            var nonUniqueAlleleNames = alleleNameLookupResults
                .GroupBy(alleleName => new { alleleName.MatchLocus, alleleName.LookupName })
                .Where(group => group.Count() > 1);

            Assert.IsEmpty(nonUniqueAlleleNames);
        }

        [TestCase(MatchLocus.A, "01:01:01:01", new[] { "01:01:01:01" }, Description = "Lookup name equals current name")]
        [TestCase(MatchLocus.A, "02:30", new[] { "02:30:01" }, Description = "2 field to 3 field")]
        [TestCase(MatchLocus.C, "07:06", new[] { "07:06:01:01" }, Description = "2 field to 4 field")]
        [TestCase(MatchLocus.B, "08:01:01", new[] { "08:01:01:01" }, Description = "3 field to 4 field")]
        [TestCase(MatchLocus.B, "07:44", new[] { "07:44N" }, Description = "Addition of expression suffix")]
        [TestCase(MatchLocus.B, "13:08Q", new[] { "13:08" }, Description = "Removal of expression suffix")]
        [TestCase(MatchLocus.A, "23:19Q", new[] { "23:19N" }, Description = "Change in expression suffix")]
        [TestCase(MatchLocus.A, "26:03:02", new[] { "26:111" }, Description = "Allele sequence renamed")]
        [TestCase(MatchLocus.Dqb1, "04:02:01:02", new[] { "04:02:01:01" }, Description = "Allele sequence identical to existing sequence")]
        [TestCase(MatchLocus.A, "02:100", new[] { "02:100" }, Description = "Reserved allele name")]
        public void AlleleNamesService_WhenExactAlleleNameHasBeenInHlaNom_CurrentAlleleNameIsAsExpected(
            MatchLocus matchLocus, string lookupName, IEnumerable<string> expectedCurrentAlleleName)
        {
            var actualAlleleName = GetAlleleNameLookupResult(matchLocus, lookupName);

            actualAlleleName.CurrentAlleleNames.ShouldBeEquivalentTo(expectedCurrentAlleleName);
        }

        [TestCase(MatchLocus.A, "02:05",
            new[] { "02:05:01:01", "02:05:01:02", "02:05:02", "02:05:03", "02:05:04", "02:05:05", "02:05:06" },
            Description = "Lookup name should return 3 & 4 field allele names")]
        [TestCase(MatchLocus.Drb1, "03:01:01",
            new[] { "03:01:01:01", "03:01:01:02", "03:01:01:03" },
            Description = "Lookup name should only return 4 field allele names")]
        [TestCase(MatchLocus.B, "39:01:01",
            new[] { "39:01:01:01", "39:01:01:02L", "39:01:01:03", "39:01:01:04", "39:01:01:05", "39:01:01:06", "39:01:01:07" },
            Description = "Lookup name without expression suffix should return names irrespective of suffix")]
        [TestCase(MatchLocus.B, "39:01:01L",
            new[] { "39:01:01:02L" },
            Description = "Lookup name with expression suffix should only return names with same suffix")]
        [TestCase(MatchLocus.A, "02:01:01:02",
            new[] { "02:01:01:02L" },
            Description = "Four field lookup name without suffix returns same allele name with suffix")]
        public void AlleleNamesService_WhenExactAlleleNameHasNeverBeenInHlaNom_ReturnAllPossibleCurrentNames(
            MatchLocus matchLocus, string lookupName, IEnumerable<string> expectedCurrentAlleleNames)
        {
            var actualAlleleName = GetAlleleNameLookupResult(matchLocus, lookupName);

            actualAlleleName.CurrentAlleleNames.ShouldBeEquivalentTo(expectedCurrentAlleleNames);
        }

        private AlleleNameLookupResult GetAlleleNameLookupResult(MatchLocus matchLocus, string lookupName)
        {
            return alleleNameLookupResults
                .First(alleleName =>
                    alleleName.MatchLocus.Equals(matchLocus) &&
                    alleleName.LookupName.Equals(lookupName));
        }
    }
}
