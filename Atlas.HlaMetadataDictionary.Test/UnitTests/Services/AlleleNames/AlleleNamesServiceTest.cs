using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.AlleleNames;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.Generators;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.AlleleNames
{
    public class AlleleNamesServiceTest
    {
        private List<IAlleleNameMetadata> alleleNameMetadata;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                var dataRepository = SharedTestDataCache.GetWmdaDataRepository();
                var historiesConsolidator = new AlleleNameHistoriesConsolidator(dataRepository);
                var fromExtractorExtractor = new AlleleNamesFromHistoriesExtractor(historiesConsolidator, dataRepository);
                var variantsExtractor = new AlleleNameVariantsExtractor(dataRepository);
                var reservedNamesExtractor = new ReservedAlleleNamesExtractor(dataRepository);

                alleleNameMetadata = new AlleleNamesService(fromExtractorExtractor, variantsExtractor, reservedNamesExtractor)
                    .GetAlleleNamesAndTheirVariants(SharedTestDataCache.HlaNomenclatureVersionForImportingTestWmdaRepositoryFiles)
                    .ToList();
            });
        }

        [Test]
        public void AlleleNamesService_GetAlleleNamesAndTheirVariants_DoesNotGenerateDuplicateAlleleNames()
        {
            var nonUniqueAlleleNames = alleleNameMetadata
                .GroupBy(alleleName => new { alleleName.Locus, alleleName.LookupName })
                .Where(group => group.Count() > 1);

            Assert.IsEmpty(nonUniqueAlleleNames);
        }

        [TestCase(Locus.A, "01:01:01:01", new[] { "01:01:01:01" }, Description = "Lookup name equals current name")]
        [TestCase(Locus.A, "02:30", new[] { "02:30:01" }, Description = "2 field to 3 field")]
        [TestCase(Locus.C, "07:06", new[] { "07:06:01:01" }, Description = "2 field to 4 field")]
        [TestCase(Locus.B, "08:01:01", new[] { "08:01:01:01" }, Description = "3 field to 4 field")]
        [TestCase(Locus.B, "07:44", new[] { "07:44N" }, Description = "Addition of expression suffix")]
        [TestCase(Locus.B, "13:08Q", new[] { "13:08" }, Description = "Removal of expression suffix")]
        [TestCase(Locus.A, "23:19Q", new[] { "23:19N" }, Description = "Change in expression suffix")]
        [TestCase(Locus.A, "26:03:02", new[] { "26:111" }, Description = "Allele sequence renamed")]
        [TestCase(Locus.Dqb1, "04:02:01:02", new[] { "04:02:01:01" }, Description = "Allele sequence identical to existing sequence")]
        [TestCase(Locus.A, "02:100", new[] { "02:100" }, Description = "Reserved allele name")]
        public void AlleleNamesService_WhenExactAlleleNameHasBeenInHlaNom_CurrentAlleleNameIsAsExpected(
            Locus locus, string lookupName, IEnumerable<string> expectedCurrentAlleleName)
        {
            var actualAlleleName = GetAlleleNameMetadata(locus, lookupName);

            actualAlleleName.CurrentAlleleNames.Should().BeEquivalentTo(expectedCurrentAlleleName);
        }

        [TestCase(Locus.A, "02:05",
            new[] { "02:05:01:01", "02:05:01:02", "02:05:02", "02:05:03", "02:05:04", "02:05:05", "02:05:06", "02:05:07" },
            Description = "Lookup name should return 3 & 4 field allele names")]
        [TestCase(Locus.Drb1, "03:01:01",
            new[] { "03:01:01:01", "03:01:01:02", "03:01:01:03" },
            Description = "Lookup name should only return 4 field allele names")]
        [TestCase(Locus.B, "39:01:01",
            new[] { "39:01:01:01", "39:01:01:02L", "39:01:01:03", "39:01:01:04", "39:01:01:05", "39:01:01:06", "39:01:01:07" },
            Description = "Lookup name without expression suffix should return names irrespective of suffix")]
        [TestCase(Locus.B, "39:01:01L",
            new[] { "39:01:01:02L" },
            Description = "Lookup name with expression suffix should only return names with same suffix")]
        [TestCase(Locus.A, "02:01:01:02",
            new[] { "02:01:01:02L" },
            Description = "Four field lookup name without suffix returns same allele name with suffix")]
        public void AlleleNamesService_WhenExactAlleleNameHasNeverBeenInHlaNom_ReturnAllPossibleCurrentNames(
            Locus locus, string lookupName, IEnumerable<string> expectedCurrentAlleleNames)
        {
            var actualAlleleName = GetAlleleNameMetadata(locus, lookupName);

            actualAlleleName.CurrentAlleleNames.Should().BeEquivalentTo(expectedCurrentAlleleNames);
        }

        private IAlleleNameMetadata GetAlleleNameMetadata(Locus locus, string lookupName)
        {
            return alleleNameMetadata
                .First(alleleName =>
                    alleleName.Locus.Equals(locus) &&
                    alleleName.LookupName.Equals(lookupName));
        }
    }
}
