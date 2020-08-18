using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
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
                donorHlaExpander = DependencyInjection.DependencyInjection.Provider.GetService<IDonorHlaExpanderFactory>()
                    .BuildForActiveHlaNomenclatureVersion();
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
                nullAlleleAsStringWithExpressingAlleleOfSameGGroup =
                    BuildTestData(nullAllele.AlleleName + "/" + expressingAlleleFromSameGGroupAsNullAllele.AlleleName);
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
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameNoSuffix.Phenotype,
                expressingAlleleFromSameGGroupAsNullAllele.DonorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameNoSuffix_VsTwoCopiesOfExpressingAllele_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = expressingAlleleFromSameGGroupAsNullAllele.Phenotype
                .SetPosition(LocusUnderTest, OtherPosition, expressingAlleleFromSameGGroupAsNullAllele.AlleleName);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameNoSuffix.Phenotype, donorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameNoSuffix_VsNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameNoSuffix.Phenotype, nullAllele.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameNoSuffix_VsDifferentNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameNoSuffix.Phenotype, differentNullAllele.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameNoSuffix_VsItself_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameNoSuffix.Phenotype,
                nullAlleleAsTwoFieldNameNoSuffix.DonorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameNoSuffix_VsNullAlleleAsTwoFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameNoSuffix.Phenotype,
                nullAlleleAsTwoFieldNameWithSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsTwoFieldNameNoSuffix_VsNullAlleleAsThreeFieldNameNoSuffix_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameNoSuffix.Phenotype,
                nullAlleleAsThreeFieldNameNoSuffix.DonorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameNoSuffix_VsNullAlleleAsThreeFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameNoSuffix.Phenotype,
                nullAlleleAsThreeFieldNameWithSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsTwoFieldNameNoSuffix_VsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameNoSuffix.Phenotype,
                nullAlleleAsStringWithExpressingAlleleOfSameGGroup.DonorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task
            Search_NullAlleleAsTwoFieldNameNoSuffix_VsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameNoSuffix.Phenotype,
                nullAlleleAsThreeFieldNameWithSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameNoSuffix_VsNullAlleleAsXxCode_ExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameNoSuffix.Phenotype, nullAlleleAsXxCode.DonorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        #endregion

        #region Two-Field Name, With Expression Letter

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameWithSuffix_VsOneCopyOfExpressingAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameWithSuffix.Phenotype,
                expressingAlleleFromSameGGroupAsNullAllele.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task FourOutOfSix_NullAlleleAsTwoFieldNameWithSuffix_VsTwoCopiesOfExpressingAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = expressingAlleleFromSameGGroupAsNullAllele.Phenotype
                .SetPosition(LocusUnderTest, OtherPosition, expressingAlleleFromSameGGroupAsNullAllele.AlleleName);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var result = await FourOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameWithSuffix.Phenotype, donorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameWithSuffix_VsNullAllele_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameWithSuffix.Phenotype, nullAllele.DonorId);

            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameWithSuffix_VsDifferentNullAllele_NullMismatchGradeAndDefiniteConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameWithSuffix.Phenotype, differentNullAllele.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.NullMismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameWithSuffix_VsItself_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameWithSuffix.Phenotype,
                nullAlleleAsTwoFieldNameWithSuffix.DonorId);

            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameWithSuffix_VsNullAlleleAsTwoFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameWithSuffix.Phenotype,
                nullAlleleAsThreeFieldNameNoSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameWithSuffix_VsNullAlleleAsThreeFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameWithSuffix.Phenotype,
                nullAlleleAsThreeFieldNameNoSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsTwoFieldNameWithSuffix_VsNullAlleleAsThreeFieldNameWithSuffix_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameWithSuffix.Phenotype,
                nullAlleleAsThreeFieldNameWithSuffix.DonorId);

            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task
            Search_NullAlleleAsTwoFieldNameWithSuffix_VsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameWithSuffix.Phenotype,
                nullAlleleAsStringWithExpressingAlleleOfSameGGroup.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsTwoFieldNameWithSuffix_VsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameWithSuffix.Phenotype,
                nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsTwoFieldNameWithSuffix_VsNullAlleleAsXxCode_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsTwoFieldNameWithSuffix.Phenotype, nullAlleleAsXxCode.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        #endregion

        #region Three-Field Name, No Expression Letter

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameNoSuffix_VsOneCopyOfExpressingAllele_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameNoSuffix.Phenotype,
                expressingAlleleFromSameGGroupAsNullAllele.DonorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task
            Search_NullAlleleAsThreeFieldNameNoSuffix_VsTwoCopiesOfExpressingAllele_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = expressingAlleleFromSameGGroupAsNullAllele.Phenotype
                .SetPosition(LocusUnderTest, OtherPosition, expressingAlleleFromSameGGroupAsNullAllele.AlleleName);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameNoSuffix.Phenotype, donorId);

            // Position under test
            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameNoSuffix_VsNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameNoSuffix.Phenotype, nullAllele.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameNoSuffix_VsDifferentNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameNoSuffix.Phenotype, differentNullAllele.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameNoSuffix_VsItself_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameNoSuffix.Phenotype,
                nullAlleleAsThreeFieldNameNoSuffix.DonorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameNoSuffix_VsNullAlleleAsTwoFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameNoSuffix.Phenotype,
                nullAlleleAsTwoFieldNameWithSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsThreeFieldNameNoSuffix_VsNullAlleleAsTwoFieldNameNoSuffix_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameNoSuffix.Phenotype,
                nullAlleleAsTwoFieldNameNoSuffix.DonorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameNoSuffix_VsNullAlleleAsThreeFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameNoSuffix.Phenotype,
                nullAlleleAsThreeFieldNameWithSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsThreeFieldNameNoSuffix_VsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameNoSuffix.Phenotype,
                nullAlleleAsStringWithExpressingAlleleOfSameGGroup.DonorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task
            Search_NullAlleleAsThreeFieldNameNoSuffix_VsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameNoSuffix.Phenotype,
                nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameNoSuffix_VsNullAlleleAsXxCode_ExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameNoSuffix.Phenotype, nullAlleleAsXxCode.DonorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        #endregion

        #region Three-Field Name, With Expression Letter

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameWithSuffix_VsOneCopyOfExpressingAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameWithSuffix.Phenotype,
                expressingAlleleFromSameGGroupAsNullAllele.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task FourOutOfSix_NullAlleleAsThreeFieldNameWithSuffix_VsTwoCopiesOfExpressingAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = expressingAlleleFromSameGGroupAsNullAllele.Phenotype
                .SetPosition(LocusUnderTest, OtherPosition, expressingAlleleFromSameGGroupAsNullAllele.AlleleName);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var result = await FourOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameWithSuffix.Phenotype, donorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameWithSuffix_VsNullAllele_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameWithSuffix.Phenotype, nullAllele.DonorId);

            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameWithSuffix_VsDifferentNullAllele_NullMismatchGradeAndDefiniteConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameWithSuffix.Phenotype, differentNullAllele.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.NullMismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameWithSuffix_VsItself_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameWithSuffix.Phenotype,
                nullAlleleAsThreeFieldNameWithSuffix.DonorId);

            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameWithSuffix_VsNullAlleleAsTwoFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameWithSuffix.Phenotype,
                nullAlleleAsTwoFieldNameNoSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameWithSuffix_VsNullAlleleAsThreeFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameWithSuffix.Phenotype,
                nullAlleleAsThreeFieldNameNoSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsThreeFieldNameWithSuffix_VsNullAlleleAsTwoFieldNameWithSuffix_NullMatchGradeAndDefiniteConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameWithSuffix.Phenotype,
                nullAlleleAsTwoFieldNameWithSuffix.DonorId);

            matchGradesForMatchingNullAlleles.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public async Task
            Search_NullAlleleAsThreeFieldNameWithSuffix_VsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameWithSuffix.Phenotype,
                nullAlleleAsStringWithExpressingAlleleOfSameGGroup.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsThreeFieldNameWithSuffix_VsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameWithSuffix.Phenotype,
                nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsThreeFieldNameWithSuffix_VsNullAlleleAsXxCode_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsThreeFieldNameWithSuffix.Phenotype, nullAlleleAsXxCode.DonorId);

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
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfSameGGroup.Phenotype,
                expressingAlleleFromSameGGroupAsNullAllele.DonorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsTwoCopiesOfExpressingAllele_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var donorPhenotype = expressingAlleleFromSameGGroupAsNullAllele.Phenotype
                .SetPosition(LocusUnderTest, OtherPosition, expressingAlleleFromSameGGroupAsNullAllele.AlleleName);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfSameGGroup.Phenotype, donorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfSameGGroup.Phenotype, nullAllele.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsDifferentNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfSameGGroup.Phenotype,
                differentNullAllele.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsItself_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfSameGGroup.Phenotype,
                nullAlleleAsStringWithExpressingAlleleOfSameGGroup.DonorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsNullAlleleAsTwoFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfSameGGroup.Phenotype,
                nullAlleleAsTwoFieldNameWithSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsNullAlleleAsTwoFieldNameNoSuffix_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfSameGGroup.Phenotype,
                nullAlleleAsTwoFieldNameNoSuffix.DonorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsNullAlleleAsThreeFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfSameGGroup.Phenotype,
                nullAlleleAsThreeFieldNameWithSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsNullAlleleAsThreeFieldNameNoSuffix_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfSameGGroup.Phenotype,
                nullAlleleAsThreeFieldNameNoSuffix.DonorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfSameGGroup.Phenotype,
                nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfSameGGroup_VsNullAlleleAsXxCode_ExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfSameGGroup.Phenotype,
                nullAlleleAsXxCode.DonorId);

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
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.Phenotype,
                expressingAlleleFromSameGGroupAsNullAllele.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            FourOutOfSix_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsTwoCopiesOfExpressingAllele_MismatchGradeAndConfidenceAssigned()
        {
            var donorPhenotype = expressingAlleleFromSameGGroupAsNullAllele.Phenotype
                .SetPosition(LocusUnderTest, OtherPosition, expressingAlleleFromSameGGroupAsNullAllele.AlleleName);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var result = await FourOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.Phenotype, donorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.Phenotype,
                nullAllele.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsDifferentNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.Phenotype,
                differentNullAllele.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsItself_ExpressingMatchGradeAndExactConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.Phenotype,
                nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.DonorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Exact);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsNullAlleleAsTwoFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.Phenotype,
                nullAlleleAsTwoFieldNameNoSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsNullAlleleAsThreeFieldNameNoSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.Phenotype,
                nullAlleleAsThreeFieldNameNoSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsNullAlleleAsTwoFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.Phenotype,
                nullAlleleAsTwoFieldNameWithSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsNullAlleleAsThreeFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.Phenotype,
                nullAlleleAsThreeFieldNameWithSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.Phenotype,
                nullAlleleAsStringWithExpressingAlleleOfSameGGroup.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_VsNullAlleleAsXxCode_ExpressingMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.Phenotype,
                nullAlleleAsXxCode.DonorId);

            matchGradesForExpressingAlleleOfSameGGroups.Should().Contain(result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        #endregion

        #region XX Code

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsOneCopyOfExpressingAllele_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsXxCode.Phenotype, expressingAlleleFromSameGGroupAsNullAllele.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsTwoCopiesOfExpressingAllele_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var donorPhenotype = expressingAlleleFromSameGGroupAsNullAllele.Phenotype
                .SetPosition(LocusUnderTest, OtherPosition, expressingAlleleFromSameGGroupAsNullAllele.AlleleName);
            var donorId = await AddDonorPhenotypeToDonorRepository(donorPhenotype);

            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsXxCode.Phenotype, donorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsXxCode.Phenotype, nullAllele.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsDifferentNullAllele_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsXxCode.Phenotype, differentNullAllele.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsItself_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsXxCode.Phenotype, nullAlleleAsXxCode.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsNullAlleleAsTwoFieldNameNoSuffix_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsXxCode.Phenotype, nullAlleleAsTwoFieldNameNoSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsNullAlleleAsTwoFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsXxCode.Phenotype, nullAlleleAsTwoFieldNameWithSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsNullAlleleAsThreeFieldNameNoSuffix_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsXxCode.Phenotype, nullAlleleAsThreeFieldNameNoSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public async Task Search_NullAlleleAsXxCode_VsNullAlleleAsThreeFieldNameWithSuffix_MismatchGradeAndConfidenceAssigned()
        {
            var result = await FiveOutOfSixSearchWithAllLociScored(nullAlleleAsXxCode.Phenotype, nullAlleleAsThreeFieldNameWithSuffix.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.Mismatch);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public async Task
            Search_NullAlleleAsXxCode_VsNullAlleleAsStringWithExpressingAlleleOfSameGGroup_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsXxCode.Phenotype,
                nullAlleleAsStringWithExpressingAlleleOfSameGGroup.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public async Task
            Search_NullAlleleAsXxCode_VsNullAlleleAsStringWithExpressingAlleleOfDifferentGGroup_GGroupMatchGradeAndPotentialConfidenceAssigned()
        {
            var result = await SixOutOfSixSearchWithAllLociScored(nullAlleleAsXxCode.Phenotype,
                nullAlleleAsStringWithExpressingAlleleOfDifferentGGroup.DonorId);

            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(MatchGrade.GGroup);
            result.SearchResultAtLocusA.ScoreDetailsAtPositionOne.MatchConfidence.Should().Be(MatchConfidence.Potential);
        }

        #endregion

        #region Helper Methods

        private async Task<int> AddDonorPhenotypeToDonorRepository(PhenotypeInfo<string> donorPhenotype, int? donorId = null)
        {
            var matchingHlaPhenotype = donorHlaExpander.ExpandDonorHlaAsync(new DonorInfo {HlaNames = donorPhenotype}).Result.MatchingHla;

            var testDonor = new DonorInfoWithTestHlaBuilder(donorId ?? DonorIdGenerator.NextId())
                .WithHla(matchingHlaPhenotype)
                .Build();

            await donorRepository.InsertBatchOfDonorsWithExpandedHla(new[] {testDonor}, false);

            return testDonor.DonorId;
        }

        private async Task<MatchingAlgorithmResult> SixOutOfSixSearchWithAllLociScored(PhenotypeInfo<string> patientPhenotype, int donorId)
        {
            var searchRequest = new SearchRequestFromHlasBuilder(patientPhenotype)
                .SixOutOfSix()
                .WithAllLociScored()
                .Build();

            var searchResults = await searchService.Search(searchRequest);
            return searchResults.Single(d => d.AtlasDonorId == donorId);
        }

        private async Task<MatchingAlgorithmResult> FiveOutOfSixSearchWithAllLociScored(PhenotypeInfo<string> patientPhenotype, int donorId)
        {
            var searchRequest = new SearchRequestFromHlasBuilder(patientPhenotype)
                .FiveOutOfSix()
                .WithSingleMismatchRequestedAt(LocusUnderTest)
                .WithAllLociScored()
                .Build();

            var searchResults = await searchService.Search(searchRequest);
            return searchResults.Single(d => d.AtlasDonorId == donorId);
        }

        private async Task<MatchingAlgorithmResult> FourOutOfSixSearchWithAllLociScored(PhenotypeInfo<string> patientPhenotype, int donorId)
        {
            var searchRequest = new SearchRequestFromHlasBuilder(patientPhenotype)
                .FourOutOfSix()
                .WithDoubleMismatchRequestedAt(LocusUnderTest)
                .WithAllLociScored()
                .Build();

            var searchResults = await searchService.Search(searchRequest);
            return searchResults.Single(d => d.AtlasDonorId == donorId);
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