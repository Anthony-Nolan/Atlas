using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.Integration.Resources.TestData;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Search
{
    /// <summary>
    /// Confirm that scoring on typings containing a null allele is as expected 
    /// when run as part of the larger search algorithm service.
    /// Only explicitly null alleles should be treated as non-expressing;
    /// typings that only include null alleles in their expanded definition, e.g., MACs, should be treated as expressing.
    /// </summary>
    public class ScoringTestsForNullAllele
    {
        private const Locus LocusUnderTest = Locus.A;
        private const LocusPosition PositionUnderTest = LocusPosition.One;

        private static readonly List<MatchGrade> ExpressingMatchGrades = new()
        {
            MatchGrade.PGroup,
            MatchGrade.GGroup,
            MatchGrade.Protein,
            MatchGrade.CDna,
            MatchGrade.GDna
        };

        private static readonly List<MatchGrade> NullMatchGrades = new()
        {
            MatchGrade.NullGDna,
            MatchGrade.NullCDna,
            MatchGrade.NullPartial
        };

        #region Test Phenotypes

        private TestPhenotype nullAllele;
        private TestPhenotype nullAlleleAs2FieldNoSuffix;
        private TestPhenotype nullAlleleAs2FieldWithSuffix;
        private TestPhenotype nullAlleleAs3FieldNoSuffix;
        private TestPhenotype nullAlleleAs3FieldWithSuffix;
        private TestPhenotype differentNullAllele;
        private TestPhenotype expressingFromNullAlleleGGroup;
        private TestPhenotype stringWithNullAndExpressingFromSameGGroup;
        private TestPhenotype stringWithNullAndExpressingFromDiffGGroups;
        private TestPhenotype macWithNullAllele;
        private TestPhenotype xxCodeWithNullAllele;
        private TestPhenotype homozygousForExpressingFromNullAlleleGGroup;
        private TestPhenotype homozygousForExpressingInOtherPosition;

        /// <summary>
        /// Phenotypes that should be treated as expressing, even though their expanded definition includes a null allele.
        /// </summary>
        private static readonly object[] AllExpressingPhenotypes = {
            nameof(expressingFromNullAlleleGGroup),
            nameof(nullAlleleAs2FieldNoSuffix),
            nameof(nullAlleleAs3FieldNoSuffix),
            nameof(stringWithNullAndExpressingFromSameGGroup),
            nameof(macWithNullAllele),
            nameof(xxCodeWithNullAllele)
        };

        /// <summary>
        /// Phenotypes that should be treated as expressing, even though their expanded definition includes a null allele,
        /// AND the expressing allele(s) in the expanded definition map to the same G group as the null allele.
        /// </summary>
        private static readonly object[] ExpressingPhenotypesFromSameGGroup = {
            nameof(expressingFromNullAlleleGGroup),
            nameof(nullAlleleAs2FieldNoSuffix),
            nameof(nullAlleleAs3FieldNoSuffix),
            nameof(stringWithNullAndExpressingFromSameGGroup),
            nameof(macWithNullAllele)
        };

        private static readonly object[] NullAlleleContainingPhenotypes = {
            nameof(nullAllele),
            nameof(nullAlleleAs2FieldWithSuffix),
            nameof(nullAlleleAs3FieldWithSuffix)
        };

        #endregion

        #region Services

        private IDonorHlaExpander donorHlaExpander;
        private IDonorUpdateRepository donorRepository;
        private ISearchService searchService;

        #endregion

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            await TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage_Async(async () =>
            {
                var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();
                donorHlaExpander = DependencyInjection.DependencyInjection.Provider.GetService<IDonorHlaExpanderFactory>()
                    .BuildForActiveHlaNomenclatureVersion();
                donorRepository = repositoryFactory.GetDonorUpdateRepository();

                await BuildAndPersistTestData();
            });
        }

        [SetUp]
        public void SetUp()
        {
            searchService = DependencyInjection.DependencyInjection.Provider.GetService<ISearchService>();
        }

        [Test]
        public async Task Search_HeterozygousPatientAndDonor_AssignsExpressingMatchScores(
            [ValueSource(nameof(AllExpressingPhenotypes))] string patientName,
            [ValueSource(nameof(AllExpressingPhenotypes))] string donorName)
        {
            var result = await SearchWithAllLociScored(0, patientName, donorName);

            var resultAtLocus = result.ScoringResult.ScoringResultsByLocus.ToLociInfo().GetLocus(LocusUnderTest);
            resultAtLocus.MatchCount = 2;
            ExpressingMatchGrades.Should().Contain(resultAtLocus.ScoreDetailsAtPositionOne.MatchGrade);
            resultAtLocus.ScoreDetailsAtPositionOne.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_HeterozygousPatient_HomozygousDonor_AssignsExpectedScores(
            [ValueSource(nameof(AllExpressingPhenotypes))] string patientName)
        {
            var result = await SearchWithAllLociScored(1, patientName, nameof(homozygousForExpressingFromNullAlleleGGroup));

            var resultAtLocus = result.ScoringResult.ScoringResultsByLocus.ToLociInfo().GetLocus(LocusUnderTest);
            resultAtLocus.MatchCount = 1;
            ExpressingMatchGrades.Should().Contain(resultAtLocus.ScoreDetailsAtPositionOne.MatchGrade);
            resultAtLocus.ScoreDetailsAtPositionOne.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_HomozygousPatient_HeterozygousDonor_AssignsExpectedScores(
            [ValueSource(nameof(AllExpressingPhenotypes))] string donorName)
        {
            var result = await SearchWithAllLociScored(1, nameof(homozygousForExpressingFromNullAlleleGGroup), donorName);

            var resultAtLocus = result.ScoringResult.ScoringResultsByLocus.ToLociInfo().GetLocus(LocusUnderTest);
            resultAtLocus.MatchCount = 1;
            ExpressingMatchGrades.Should().Contain(resultAtLocus.ScoreDetailsAtPositionOne.MatchGrade);
            resultAtLocus.ScoreDetailsAtPositionOne.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAllelesInPatientAndDonor_AssignsNullMatchScores(
            [ValueSource(nameof(NullAlleleContainingPhenotypes))] string patientName,
            [ValueSource(nameof(NullAlleleContainingPhenotypes))] string donorName)
        {
            var result = await SearchWithAllLociScored(0, patientName, donorName);

            var resultAtLocus = result.ScoringResult.ScoringResultsByLocus.ToLociInfo().GetLocus(LocusUnderTest);
            resultAtLocus.MatchCount = 2;
            NullMatchGrades.Should().Contain(resultAtLocus.ScoreDetailsAtPositionOne.MatchGrade);
            resultAtLocus.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_NullAllelesDoNotShareGGroup_AssignsNullMismatchGrade(
            [ValueSource(nameof(NullAlleleContainingPhenotypes))] string patientName)
        {
            var result = await SearchWithAllLociScored(0, patientName, nameof(differentNullAllele));

            var resultAtLocus = result.ScoringResult.ScoringResultsByLocus.ToLociInfo().GetLocus(LocusUnderTest);
            resultAtLocus.MatchCount = 2;
            resultAtLocus.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.NullMismatch);
            resultAtLocus.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_ExpressingDoNotSharePGroup_AssignsMismatchScores(
            [ValueSource(nameof(ExpressingPhenotypesFromSameGGroup))] string patientName)
        {
            var result = await SearchWithAllLociScored(1, patientName, nameof(stringWithNullAndExpressingFromDiffGGroups));

            var resultAtLocus = result.ScoringResult.ScoringResultsByLocus.ToLociInfo().GetLocus(LocusUnderTest);
            resultAtLocus.MatchCount = 1;
            resultAtLocus.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            resultAtLocus.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_HomozygousPatient_MismatchedNullAlleleDonor_AssignsExpressingVsNullGrade(
            [ValueSource(nameof(NullAlleleContainingPhenotypes))] string donorName)
        {
            var result = await SearchWithAllLociScored(2, nameof(homozygousForExpressingFromNullAlleleGGroup), donorName);

            var resultAtLocus = result.ScoringResult.ScoringResultsByLocus.ToLociInfo().GetLocus(LocusUnderTest);
            resultAtLocus.MatchCount = 0;
            resultAtLocus.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.ExpressingVsNull);
            resultAtLocus.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAllelePatient_MismatchedHomozygousDonor_AssignsExpressingVsNullGrade(
            [ValueSource(nameof(NullAlleleContainingPhenotypes))] string patientName)
        {
            var result = await SearchWithAllLociScored(2, patientName, nameof(homozygousForExpressingFromNullAlleleGGroup));

            var resultAtLocus = result.ScoringResult.ScoringResultsByLocus.ToLociInfo().GetLocus(LocusUnderTest);
            resultAtLocus.MatchCount = 0;
            resultAtLocus.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.ExpressingVsNull);
            resultAtLocus.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_HomozygousPatient_MatchedNullAlleleDonor_AssignsExpressingVsNullGrade(
            [ValueSource(nameof(NullAlleleContainingPhenotypes))] string donorName)
        {
            var result = await SearchWithAllLociScored(0, nameof(homozygousForExpressingInOtherPosition), donorName);

            var resultAtLocus = result.ScoringResult.ScoringResultsByLocus.ToLociInfo().GetLocus(LocusUnderTest);
            resultAtLocus.MatchCount = 2;
            resultAtLocus.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.ExpressingVsNull);
            resultAtLocus.ScoreDetailsAtPositionOne.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAllelePatient_MatchedHomozygousDonor_AssignsExpressingVsNullGrade(
            [ValueSource(nameof(NullAlleleContainingPhenotypes))] string patientName)
        {
            var result = await SearchWithAllLociScored(0, patientName, nameof(homozygousForExpressingInOtherPosition));

            var resultAtLocus = result.ScoringResult.ScoringResultsByLocus.ToLociInfo().GetLocus(LocusUnderTest);
            resultAtLocus.MatchCount = 2;
            resultAtLocus.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.ExpressingVsNull);
            resultAtLocus.ScoreDetailsAtPositionOne.MatchConfidence.Should().NotBe(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_HeterozygousPatient_NullAlleleDonor_AssignsExpressingVsNullGrade(
            [ValueSource(nameof(AllExpressingPhenotypes))] string patientName,
            [ValueSource(nameof(NullAlleleContainingPhenotypes))] string donorName)
        {
            var result = await SearchWithAllLociScored(1, patientName, donorName);

            var resultAtLocus = result.ScoringResult.ScoringResultsByLocus.ToLociInfo().GetLocus(LocusUnderTest);
            resultAtLocus.MatchCount = 1;
            resultAtLocus.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.ExpressingVsNull);
            resultAtLocus.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAllelePatient_HeterozygousDonor_AssignsExpressingVsNullGrade(
            [ValueSource(nameof(AllExpressingPhenotypes))] string patientName,
            [ValueSource(nameof(NullAlleleContainingPhenotypes))] string donorName)
        {
            var result = await SearchWithAllLociScored(1, patientName, donorName);

            var resultAtLocus = result.ScoringResult.ScoringResultsByLocus.ToLociInfo().GetLocus(LocusUnderTest);
            resultAtLocus.MatchCount = 1;
            resultAtLocus.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.ExpressingVsNull);
            resultAtLocus.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        #region Helper Methods

        private async Task BuildAndPersistTestData()
        {
            const string expressingAlleleName = "01:01:01:01";
            const string nullAlleleName = "01:01:01:02N";

            // Matching & scoring assertions are based on the following assumptions:
            // In v.3.3.0 of HLA db, the null allele below is the only null member of the group of alleles beginning with the same first two fields.
            // Therefore, the two- and three-field truncated name variants - WITH suffix - should only map this null allele.
            // The truncated name variants that have NO suffix should return the relevant expressing alleles, as well as the null allele.
            expressingFromNullAlleleGGroup = new TestPhenotype(expressingAlleleName);
            nullAllele = new TestPhenotype(nullAlleleName);
            nullAlleleAs2FieldNoSuffix = new TestPhenotype("01:01");
            nullAlleleAs2FieldWithSuffix = new TestPhenotype("01:01N");
            nullAlleleAs3FieldNoSuffix = new TestPhenotype("01:01:01");
            nullAlleleAs3FieldWithSuffix = new TestPhenotype("01:01:01N");
            stringWithNullAndExpressingFromSameGGroup = new TestPhenotype($"{nullAlleleName}/{expressingAlleleName}");
            stringWithNullAndExpressingFromDiffGGroups = new TestPhenotype($"{nullAlleleName}/01:09:01:01");
            macWithNullAllele = new TestPhenotype("01:BMMP"); // expands to 01:01/01:01N
            xxCodeWithNullAllele = new TestPhenotype("01:XX");
            differentNullAllele = new TestPhenotype("03:01:01:02N");

            homozygousForExpressingFromNullAlleleGGroup = expressingFromNullAlleleGGroup.MakeTestLocusHomozygous(LocusPosition.One);
            homozygousForExpressingInOtherPosition = expressingFromNullAlleleGGroup.MakeTestLocusHomozygous(LocusPosition.Two);

            var allTestData = new[]
            {
                expressingFromNullAlleleGGroup,
                nullAllele,
                nullAlleleAs2FieldNoSuffix,
                nullAlleleAs2FieldWithSuffix,
                nullAlleleAs3FieldNoSuffix,
                nullAlleleAs3FieldWithSuffix,
                stringWithNullAndExpressingFromSameGGroup,
                stringWithNullAndExpressingFromDiffGGroups,
                macWithNullAllele,
                xxCodeWithNullAllele,
                differentNullAllele,
                homozygousForExpressingFromNullAlleleGGroup,
                homozygousForExpressingInOtherPosition
            };

            var donors = await Task.WhenAll(allTestData.Select(GetDonorInfo));
            await AddDonorPhenotypeToDonorRepository(donors);
        }

        private async Task<DonorInfoWithExpandedHla> GetDonorInfo(TestPhenotype alleleTestData)
        {
            var matchingHlaPhenotype = (await donorHlaExpander.ExpandDonorHlaAsync(new DonorInfo { HlaNames = alleleTestData.Phenotype })).MatchingHla;

            return new DonorInfoWithTestHlaBuilder(alleleTestData.DonorId)
                .WithHla(matchingHlaPhenotype)
                .Build();
        }

        private async Task AddDonorPhenotypeToDonorRepository(IEnumerable<DonorInfoWithExpandedHla> donors)
        {
            await donorRepository.InsertBatchOfDonorsWithExpandedHla(donors, false);
        }

        private async Task<MatchingAlgorithmResult> SearchWithAllLociScored(int mismatchCount, string patientName, string donorName)
        {
            var patientPhenotype = TestDataSelector(patientName).Phenotype;
            var donorId = TestDataSelector(donorName).DonorId;

            var searchRequest = mismatchCount switch
            {
                0 => new SearchRequestFromHlasBuilder(patientPhenotype)
                    .SixOutOfSix()
                    .WithAllLociScored()
                    .Build(),
                1 => new SearchRequestFromHlasBuilder(patientPhenotype)
                    .FiveOutOfSix()
                    .WithSingleMismatchRequestedAt(LocusUnderTest)
                    .WithAllLociScored()
                    .Build(),
                2 => new SearchRequestFromHlasBuilder(patientPhenotype)
                    .FourOutOfSix()
                    .WithDoubleMismatchRequestedAt(LocusUnderTest)
                    .WithAllLociScored()
                    .Build(),
                _ => throw new ArgumentException($"Mismatch count of {mismatchCount} is not supported")
            };

            var searchResults = await searchService.Search(searchRequest);
            return searchResults.Single(d => d.AtlasDonorId == donorId);
        }

        private TestPhenotype TestDataSelector(string dataName)
        {
            return dataName switch
            {
                nameof(expressingFromNullAlleleGGroup) => expressingFromNullAlleleGGroup,
                nameof(nullAllele) => nullAllele,
                nameof(nullAlleleAs2FieldNoSuffix) => nullAlleleAs2FieldNoSuffix,
                nameof(nullAlleleAs2FieldWithSuffix) => nullAlleleAs2FieldWithSuffix,
                nameof(nullAlleleAs3FieldNoSuffix) => nullAlleleAs3FieldNoSuffix,
                nameof(nullAlleleAs3FieldWithSuffix) => nullAlleleAs3FieldWithSuffix,
                nameof(stringWithNullAndExpressingFromSameGGroup) => stringWithNullAndExpressingFromSameGGroup,
                nameof(stringWithNullAndExpressingFromDiffGGroups) => stringWithNullAndExpressingFromDiffGGroups,
                nameof(macWithNullAllele) => macWithNullAllele,
                nameof(xxCodeWithNullAllele) => xxCodeWithNullAllele,
                nameof(differentNullAllele) => differentNullAllele,
                nameof(homozygousForExpressingFromNullAlleleGGroup) => homozygousForExpressingFromNullAlleleGGroup,
                nameof(homozygousForExpressingInOtherPosition) => homozygousForExpressingInOtherPosition,
                _ => throw new ArgumentException($"No test data found for {dataName}")
            };
        }
        #endregion

        private class TestPhenotype
        {
            public PhenotypeInfo<string> Phenotype { get; private init; }
            public int DonorId { get; } = DonorIdGenerator.NextId();

            public TestPhenotype(string hlaName)
            {
                var defaultPhenotype = new SampleTestHlas.HeterozygousSet1().SixLocus_SingleExpressingAlleles;
                Phenotype = defaultPhenotype.SetPosition(LocusUnderTest, PositionUnderTest, hlaName);
            }

            private TestPhenotype() { }

            public TestPhenotype MakeTestLocusHomozygous(LocusPosition positionToDuplicate)
            {
                var hlaName = Phenotype.GetPosition(LocusUnderTest, positionToDuplicate);

                return new TestPhenotype { Phenotype = Phenotype.SetLocus(LocusUnderTest, hlaName) };
            }
        }
    }
}