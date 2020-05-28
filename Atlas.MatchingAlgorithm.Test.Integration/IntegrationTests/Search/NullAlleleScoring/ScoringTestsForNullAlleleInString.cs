using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults.PerLocus;
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

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Search.NullAlleleScoring
{
    /// <summary>
    /// Confirm that scoring on allele strings with a null allele is as expected 
    /// when run as part of the larger search algorithm service.
    /// This fixture focuses on one locus with an allele string typing at one position.
    /// </summary>
    public class ScoringTestsForNullAlleleInString
    {
        private const Locus LocusUnderTest = Locus.A;
        private const LocusPosition PositionUnderTest = LocusPosition.One;
        private const LocusPosition OtherPosition = LocusPosition.Two;

        private readonly List<MatchGrade> matchGradesForExpressingAlleleOfSameGGroups = new List<MatchGrade>
        {
            MatchGrade.PGroup,
            MatchGrade.GGroup,
            MatchGrade.Protein,
            MatchGrade.CDna,
            MatchGrade.GDna
        };

        private readonly List<MatchGrade> matchGradesForMatchingNullAlleles = new List<MatchGrade>
        {
            MatchGrade.NullGDna,
            MatchGrade.NullCDna,
            MatchGrade.NullPartial
        };

        private AlleleTestData expressingAlleleFromSameGGroupAsNullAllele;
        private AlleleTestData nullAllele;
        private AlleleTestData nullAlleleAsTwoFieldNameNoSuffix;
        private AlleleTestData nullAlleleAsTwoFieldNameWithSuffix;
        private AlleleTestData nullAlleleAsThreeFieldNameNoSuffix;
        private AlleleTestData nullAlleleAsThreeFieldNameWithSuffix;
        private AlleleTestData nullAlleleAsStringWithExpressingAlleleOfSameGGroup;
        private AlleleTestData nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup;
        private AlleleTestData nullAlleleAsXxCode;
        private AlleleTestData differentNullAllele;

        private IDonorHlaExpander donorHlaExpander;
        private IDonorUpdateRepository donorRepository;
        private ISearchService searchService;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            await TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage_Async(async () =>
            {

                var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();
                donorHlaExpander = DependencyInjection.DependencyInjection.Provider.GetService<IDonorHlaExpanderFactory>().BuildForActiveHlaNomenclatureVersion();
                donorRepository = repositoryFactory.GetDonorUpdateRepository();

                // Matching & scoring assertions are based on the following assumptions:
                // In v.3.3.0 of HLA db, the null allele below is the only null member of the group of alleles beginning with the same first two fields.
                // Therefore, the two- and three-field truncated name variants - WITH suffix - should only map this null allele.
                // The truncated name variants that have NO suffix should return the relevant expressing alleles, as well as the null allele.
                expressingAlleleFromSameGGroupAsNullAllele = BuildTestData("01:01:01:01");
                nullAllele = BuildTestData("01:01:01:02N");
                nullAlleleAsTwoFieldNameNoSuffix = BuildTestData("01:01");
                nullAlleleAsTwoFieldNameWithSuffix = BuildTestData("01:01N");
                nullAlleleAsThreeFieldNameNoSuffix = BuildTestData("01:01:01");
                nullAlleleAsThreeFieldNameWithSuffix = BuildTestData("01:01:01N");
                nullAlleleAsStringWithExpressingAlleleOfSameGGroup = BuildTestData(nullAllele.AlleleName + "/" + expressingAlleleFromSameGGroupAsNullAllele.AlleleName);
                nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup = BuildTestData(nullAllele.AlleleName + "/01:09:01:01");
                nullAlleleAsXxCode = BuildTestData("01:XX");
                differentNullAllele = BuildTestData("03:01:01:02N");

                var allTestAlleles = new[]
                {
                    expressingAlleleFromSameGGroupAsNullAllele,
                    nullAllele,
                    nullAlleleAsTwoFieldNameNoSuffix,
                    nullAlleleAsTwoFieldNameWithSuffix,
                    nullAlleleAsThreeFieldNameNoSuffix,
                    nullAlleleAsThreeFieldNameWithSuffix,
                    nullAlleleAsStringWithExpressingAlleleOfSameGGroup,
                    nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup,
                    nullAlleleAsXxCode,
                    differentNullAllele,
                };

                foreach (var testAllele in allTestAlleles)
                {
                    await AddDonorPhenotypeToDonorRepository(testAllele.Phenotype, testAllele.DonorId);
                }
            });
        }

        private AlleleTestData BuildTestData(string alleleName)
        {
            return new AlleleTestData(donorHlaExpander, alleleName, DonorIdGenerator.NextId());
        }

        [SetUp]
        public void ResolveSearchService()
        {
            searchService = DependencyInjection.DependencyInjection.Provider.GetService<ISearchService>();
        }

        #region Two-Field Name, No Expression Letter

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameNoSuffix_VsOneCopyOfExpressingAllele_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsTwoFieldNameNoSuffix, expressingAlleleFromSameGGroupAsNullAllele);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameNoSuffix_VsTwoCopiesOfExpressingAllele_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = expressingAlleleFromSameGGroupAsNullAllele.Phenotype;
            donorPhenotype.SetPosition(LocusUnderTest, OtherPosition, expressingAlleleFromSameGGroupAsNullAllele.AlleleName);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(nullAlleleAsTwoFieldNameNoSuffix.Phenotype)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var result = (await searchService.Search(searchRequest)).Single(d => d.DonorId == donorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameNoSuffix_VsNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsTwoFieldNameNoSuffix, nullAllele);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameNoSuffix_VsDifferentNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsTwoFieldNameNoSuffix, differentNullAllele);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameNoSuffix_VsItself_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsTwoFieldNameNoSuffix, nullAlleleAsTwoFieldNameNoSuffix);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameNoSuffix_VsNullAlleleAsTwoFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsTwoFieldNameNoSuffix, nullAlleleAsTwoFieldNameWithSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameNoSuffix_VsNullAlleleAsThreeFieldNameNoSuffix_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsTwoFieldNameNoSuffix, nullAlleleAsThreeFieldNameNoSuffix);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameNoSuffix_VsNullAlleleAsThreeFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsTwoFieldNameNoSuffix, nullAlleleAsThreeFieldNameWithSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsTwoFieldNameNoSuffix_VsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsTwoFieldNameNoSuffix, nullAlleleAsStringWithExpressingAlleleOfSameGGroup);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task
            Search_NullAlleleAsTwoFieldNameNoSuffix_VsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsTwoFieldNameNoSuffix, nullAlleleAsThreeFieldNameWithSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameNoSuffix_VsNullAlleleAsXxCode_ExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsTwoFieldNameNoSuffix, nullAlleleAsXxCode);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        #endregion

        #region Two-Field Name, With Expression Letter

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameWithSuffix_VsOneCopyOfExpressingAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsTwoFieldNameWithSuffix, expressingAlleleFromSameGGroupAsNullAllele);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task FourOutOfSix_NullAlleleAsTwoFieldNameWithSuffix_VsTwoCopiesOfExpressingAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = expressingAlleleFromSameGGroupAsNullAllele.Phenotype;
            donorPhenotype.SetPosition(LocusUnderTest, OtherPosition, expressingAlleleFromSameGGroupAsNullAllele.AlleleName);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(nullAlleleAsTwoFieldNameWithSuffix.Phenotype)
                .FourOutOfSix()
                .WithDoubleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var result = (await searchService.Search(searchRequest)).Single(d => d.DonorId == donorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameWithSuffix_VsNullAllele_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsTwoFieldNameWithSuffix, nullAllele);

            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameWithSuffix_VsDifferentNullAllele_NullMismatchGradeAndDefiniteConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsTwoFieldNameWithSuffix, differentNullAllele);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.NullMismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameWithSuffix_VsItself_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsTwoFieldNameWithSuffix, nullAlleleAsTwoFieldNameWithSuffix);

            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameWithSuffix_VsNullAlleleAsTwoFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsTwoFieldNameWithSuffix, nullAlleleAsThreeFieldNameNoSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameWithSuffix_VsNullAlleleAsThreeFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsTwoFieldNameWithSuffix, nullAlleleAsThreeFieldNameNoSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameWithSuffix_VsNullAlleleAsThreeFieldNameWithSuffix_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsTwoFieldNameWithSuffix, nullAlleleAsThreeFieldNameWithSuffix);

            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameWithSuffix_VsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsTwoFieldNameWithSuffix, nullAlleleAsStringWithExpressingAlleleOfSameGGroup);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameWithSuffix_VsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsTwoFieldNameWithSuffix, nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameWithSuffix_VsNullAlleleAsXxCode_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsTwoFieldNameWithSuffix, nullAlleleAsXxCode);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        #endregion

        #region Three-Field Name, No Expression Letter

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameNoSuffix_VsOneCopyOfExpressingAllele_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsThreeFieldNameNoSuffix, expressingAlleleFromSameGGroupAsNullAllele);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task
            Search_NullAlleleAsThreeFieldNameNoSuffix_VsTwoCopiesOfExpressingAllele_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = expressingAlleleFromSameGGroupAsNullAllele.Phenotype;
            donorPhenotype.SetPosition(LocusUnderTest, OtherPosition, expressingAlleleFromSameGGroupAsNullAllele.AlleleName);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(nullAlleleAsThreeFieldNameNoSuffix.Phenotype)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameNoSuffix_VsNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsThreeFieldNameNoSuffix, nullAllele);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameNoSuffix_VsDifferentNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsThreeFieldNameNoSuffix, differentNullAllele);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameNoSuffix_VsItself_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsThreeFieldNameNoSuffix, nullAlleleAsThreeFieldNameNoSuffix);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameNoSuffix_VsNullAlleleAsTwoFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsThreeFieldNameNoSuffix, nullAlleleAsTwoFieldNameWithSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameNoSuffix_VsNullAlleleAsTwoFieldNameNoSuffix_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsThreeFieldNameNoSuffix, nullAlleleAsTwoFieldNameNoSuffix);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameNoSuffix_VsNullAlleleAsThreeFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsThreeFieldNameNoSuffix, nullAlleleAsThreeFieldNameWithSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameNoSuffix_VsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsThreeFieldNameNoSuffix, nullAlleleAsStringWithExpressingAlleleOfSameGGroup);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameNoSuffix_VsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsThreeFieldNameNoSuffix, nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameNoSuffix_VsNullAlleleAsXxCode_ExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsThreeFieldNameNoSuffix, nullAlleleAsXxCode);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        #endregion

        #region Three-Field Name, With Expression Letter

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameWithSuffix_VsOneCopyOfExpressingAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsThreeFieldNameWithSuffix, expressingAlleleFromSameGGroupAsNullAllele);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task FourOutOfSix_NullAlleleAsThreeFieldNameWithSuffix_VsTwoCopiesOfExpressingAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = expressingAlleleFromSameGGroupAsNullAllele.Phenotype;
            donorPhenotype.SetPosition(LocusUnderTest, OtherPosition, expressingAlleleFromSameGGroupAsNullAllele.AlleleName);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(nullAlleleAsThreeFieldNameWithSuffix.Phenotype)
                .FourOutOfSix()
                .WithDoubleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameWithSuffix_VsNullAllele_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsThreeFieldNameWithSuffix, nullAllele);

            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameWithSuffix_VsDifferentNullAllele_NullMismatchGradeAndDefiniteConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsThreeFieldNameWithSuffix, differentNullAllele);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.NullMismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameWithSuffix_VsItself_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsThreeFieldNameWithSuffix, nullAlleleAsThreeFieldNameWithSuffix);

            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameWithSuffix_VsNullAlleleAsTwoFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsThreeFieldNameWithSuffix, nullAlleleAsTwoFieldNameNoSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameWithSuffix_VsNullAlleleAsThreeFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsThreeFieldNameWithSuffix, nullAlleleAsThreeFieldNameNoSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameWithSuffix_VsNullAlleleAsTwoFieldNameWithSuffix_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsThreeFieldNameWithSuffix, nullAlleleAsTwoFieldNameWithSuffix);

            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameWithSuffix_VsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsThreeFieldNameWithSuffix, nullAlleleAsStringWithExpressingAlleleOfSameGGroup);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameWithSuffix_VsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsThreeFieldNameWithSuffix, nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameWithSuffix_VsNullAlleleAsXxCode_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsThreeFieldNameWithSuffix, nullAlleleAsXxCode);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        #endregion
        //
        #region Allele String, With Expressing Allele From Same G Group

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsOneCopyOfExpressingAllele_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfSameGGroup, expressingAlleleFromSameGGroupAsNullAllele);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsTwoCopiesOfExpressingAllele_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = expressingAlleleFromSameGGroupAsNullAllele.Phenotype;
            donorPhenotype.SetPosition(LocusUnderTest, OtherPosition, expressingAlleleFromSameGGroupAsNullAllele.AlleleName);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(nullAlleleAsStringWithExpressingAlleleOfSameGGroup.Phenotype)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfSameGGroup, nullAllele);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsDifferentNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfSameGGroup, differentNullAllele);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsItself_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfSameGGroup, nullAlleleAsStringWithExpressingAlleleOfSameGGroup);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsNullAlleleAsTwoFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfSameGGroup, nullAlleleAsTwoFieldNameWithSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsNullAlleleAsTwoFieldNameNoSuffix_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfSameGGroup, nullAlleleAsTwoFieldNameNoSuffix);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsNullAlleleAsThreeFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfSameGGroup, nullAlleleAsThreeFieldNameWithSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsNullAlleleAsThreeFieldNameNoSuffix_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfSameGGroup, nullAlleleAsThreeFieldNameNoSuffix);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfSameGGroup, nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsNullAlleleAsXxCode_ExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfSameGGroup, nullAlleleAsXxCode);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        #endregion
        //
        #region Allele String, With Expressing Allele From Different G Group

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsOneCopyOfExpressingAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup, expressingAlleleFromSameGGroupAsNullAllele);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            FourOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsTwoCopiesOfExpressingAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = expressingAlleleFromSameGGroupAsNullAllele.Phenotype;
            donorPhenotype.SetPosition(LocusUnderTest, OtherPosition, expressingAlleleFromSameGGroupAsNullAllele.AlleleName);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.Phenotype)
                .FourOutOfSix()
                .WithDoubleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.Single(d => d.DonorId == donorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup, nullAllele);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsDifferentNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup, differentNullAllele);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsItself_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup, nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsNullAlleleAsTwoFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup, nullAlleleAsTwoFieldNameNoSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsNullAlleleAsThreeFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup, nullAlleleAsThreeFieldNameNoSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsNullAlleleAsTwoFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup, nullAlleleAsTwoFieldNameWithSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsNullAlleleAsThreeFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup, nullAlleleAsThreeFieldNameWithSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup, nullAlleleAsStringWithExpressingAlleleOfSameGGroup);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsNullAlleleAsXxCode_ExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup, nullAlleleAsXxCode);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        #endregion

        #region XX Code

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsOneCopyOfExpressingAllele_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsXxCode, expressingAlleleFromSameGGroupAsNullAllele);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsTwoCopiesOfExpressingAllele_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = expressingAlleleFromSameGGroupAsNullAllele.Phenotype;
            donorPhenotype.SetPosition(LocusUnderTest, OtherPosition, expressingAlleleFromSameGGroupAsNullAllele.AlleleName);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var searchRequest = new SearchRequestFromHlasBuilder(nullAlleleAsXxCode.Phenotype)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();

            var result = (await searchService.Search(searchRequest)).Single(d => d.DonorId == donorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsXxCode, nullAllele);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsDifferentNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsXxCode, differentNullAllele);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsItself_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsXxCode, nullAlleleAsXxCode);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsNullAlleleAsTwoFieldNameNoSuffix_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsXxCode, nullAlleleAsTwoFieldNameNoSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsNullAlleleAsTwoFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsXxCode, nullAlleleAsTwoFieldNameWithSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsNullAlleleAsThreeFieldNameNoSuffix_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsXxCode, nullAlleleAsThreeFieldNameNoSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsNullAlleleAsThreeFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearch(nullAlleleAsXxCode, nullAlleleAsThreeFieldNameWithSuffix);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsXxCode_VsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsXxCode, nullAlleleAsStringWithExpressingAlleleOfSameGGroup);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public async Task
            Search_NullAlleleAsXxCode_VsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearch(nullAlleleAsXxCode, nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        #endregion

        #region Helper Methods

        private async Task<int> AddDonorPhenotypeToDonorRepository(PhenotypeInfo<string> donorPhenotype, int? donorId = null)
        {
            var matchingHlaPhenotype = donorHlaExpander.ExpandDonorHlaAsync(new DonorInfo { HlaNames = donorPhenotype }).Result.MatchingHla;

            var testDonor = new DonorInfoWithTestHlaBuilder(donorId ?? DonorIdGenerator.NextId())
                .WithHla(matchingHlaPhenotype)
                .Build();

            await donorRepository.InsertBatchOfDonorsWithExpandedHla(new[] { testDonor });

            return testDonor.DonorId;
        }

        private async Task<SearchResult> SixOutOfSixSearch(AlleleTestData patientAllele, AlleleTestData donorAllele)
        {
            var searchRequest = new SearchRequestFromHlasBuilder(patientAllele.Phenotype).SixOutOfSix().Build();
            var searchResults = await searchService.Search(searchRequest);
            return searchResults.Single(d => d.DonorId == donorAllele.DonorId);
        }

        private async Task<SearchResult> FiveOutOfSixSearch(AlleleTestData patientAllele, AlleleTestData donorAllele)
        {
            var searchRequest = new SearchRequestFromHlasBuilder(patientAllele.Phenotype)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .Build();
            var searchResults = await searchService.Search(searchRequest);
            return searchResults.Single(d => d.DonorId == donorAllele.DonorId);
        }

        #endregion

        private class AlleleTestData
        {
            public string AlleleName { get; }
            public PhenotypeInfo<string> Phenotype { get; }
            private DonorInfoWithExpandedHla Donor { get; }
            public int DonorId => Donor.DonorId;


            public AlleleTestData(IDonorHlaExpander donorHlaExpander, string alleleName, int donorId)
            {
                AlleleName = alleleName;
                Phenotype = BuildPhenotype(alleleName);
                Donor = BuildDonor(donorHlaExpander, donorId);
            }

            private static PhenotypeInfo<string> BuildPhenotype(string hlaForPositionUnderTest)
            {
                var defaultPhenotype = new SampleTestHlas.HeterozygousSet1().SixLocus_SingleExpressingAlleles;
                return defaultPhenotype.Map((l, p, hla) => l == LocusUnderTest && p == PositionUnderTest ? hlaForPositionUnderTest : hla);
            }

            private DonorInfoWithExpandedHla BuildDonor(IDonorHlaExpander donorHlaExpander, int donorId)
            {
                var expandedDonor = donorHlaExpander.ExpandDonorHlaAsync(new DonorInfo {HlaNames = Phenotype}).Result;

                return new DonorInfoWithTestHlaBuilder(donorId)
                    .WithHla(expandedDonor.MatchingHla)
                    .Build();
            }

        }
    }
}