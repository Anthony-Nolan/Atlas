using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Test.Integration.TestData;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Matching
{
    /// <summary>
    /// Check that the matching service returns expected donors when
    /// either patient and/or donor has a null allele within an allele string typing.
    /// Only one locus is under test to keep things simple.
    /// </summary>
    public class MatchingTestsForNullAlleleInString : IntegrationTestBase
    {
        private const Locus LocusUnderTest = Locus.A;
        private const DonorType MatchingDonorType = DonorType.Adult;

        private const string NullAllele = "01:11N";
        private const string DifferentNullAllele = "11:21N";
        private const string ExpressingAllele = "02:04";
        private const string NullRelatedToExpressingAllele = "02:32N";
        private const string DifferentExpressingAllele = "03:08";

        private const string NullAlleleInString1 = NullAllele + "/01:07";
        private const string NullAlleleInString2 = NullAllele + "/01:23";
        private const string DifferentNullAlleleInString = DifferentNullAllele + "/11:06";
        private const string ExpressingAlleleInString1 = ExpressingAllele + "/02:33";
        private const string ExpressingAlleleInString2 = ExpressingAllele + "/02:29";
        private const string ExpressingAlleleInString3 = ExpressingAllele + "/02:11";
        private const string ExpressingAlleleAndRelatedNullInString = ExpressingAllele + "/" + NullRelatedToExpressingAllele;
        private const string NullRelatedToExpressingAlleleInString = NullRelatedToExpressingAllele + "/02:33";
        private const string DifferentExpressingAlleleInString = DifferentExpressingAllele + "/03:14";

        private PhenotypeInfo<string> originalHlaPhenotype;
        private AlleleLevelMatchCriteriaFromExpandedHla criteriaFromExpandedHla;
        private IExpandHlaPhenotypeService expandHlaPhenotypeService;
        private IDonorImportRepository donorImportRepository;
        private IDonorMatchingService donorMatchingService;

        private PhenotypeInfo<ExpandedHla> patientWithNullAlleleInStringAndExpressingAllele;
        private PhenotypeInfo<ExpandedHla> patientWithTwoCopiesOfExpressingAllele;
        private PhenotypeInfo<ExpandedHla> patientWithExpressingAlleleInStringAndNullAlleleInString;
        private PhenotypeInfo<ExpandedHla> patientWithTwoCopiesOfExpressingAlleleInStrings;
        private PhenotypeInfo<ExpandedHla> patientWithTwoNullAllelesInTwoStrings;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            originalHlaPhenotype = new TestHla.HeterozygousSet1().SixLocus_SingleExpressingAlleles;
            criteriaFromExpandedHla = new AlleleLevelMatchCriteriaFromExpandedHla(LocusUnderTest, MatchingDonorType);
            expandHlaPhenotypeService = Container.Resolve<IExpandHlaPhenotypeService>();
            donorImportRepository = Container.Resolve<IDonorImportRepository>();

            BuildPatientPhenotypes();
        }

        [SetUp]
        public void SetUpBeforeEachTest()
        {
            donorMatchingService = Container.Resolve<IDonorMatchingService>();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ClearDatabase();
        }

        #region Allele String vs. Single Allele

        [TestCase(NullAllele, ExpressingAllele)]
        [TestCase(ExpressingAllele, NullAllele)]
        public async Task Search_WithOneAllowedMismatch_PatientWithNullAlleleInString_MatchesDonorWithSameNullAlleleAndSameExpressingAllele(
            string donorHla1,
            string donorHla2)
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(donorHla1, donorHla2);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithNullAlleleInStringAndExpressingAllele, 1);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [TestCase(DifferentNullAllele, ExpressingAllele)]
        [TestCase(ExpressingAllele, DifferentNullAllele)]
        public async Task Search_WithOneAllowedMismatch_PatientWithNullAlleleInString_MatchesDonorWithSameExpressingAlleleButDifferentNullAllele(
            string donorHla1,
            string donorHla2)
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(donorHla1, donorHla2);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithNullAlleleInStringAndExpressingAllele, 1);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatch_PatientWithNullAlleleInString_MatchesDonorWithTwoCopiesOfSameExpressingAllele()
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(ExpressingAllele, ExpressingAllele);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithNullAlleleInStringAndExpressingAllele, 1);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [TestCase(NullAllele, DifferentExpressingAllele)]
        [TestCase(DifferentExpressingAllele, NullAllele)]
        public async Task Search_WithTwoAllowedMismatches_PatientWithNullAlleleInString_MatchesDonorWithSameNullAlleleButDifferentExpressingAllele(
            string donorHla1,
            string donorHla2)
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(donorHla1, donorHla2);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithNullAlleleInStringAndExpressingAllele, 2);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [TestCase(DifferentNullAllele, DifferentExpressingAllele)]
        [TestCase(DifferentExpressingAllele, DifferentNullAllele)]
        public async Task Search_WithTwoAllowedMismatches_PatientWithNullAlleleInString_MatchesDonorWithDifferentNullAlleleAndDifferentExpressingAllele(
            string donorHla1,
            string donorHla2)
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(donorHla1, donorHla2);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithNullAlleleInStringAndExpressingAllele, 2);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatches_PatientWithNullAlleleInString_MatchesDonorWithTwoCopiesOfDifferentExpressingAllele()
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(DifferentExpressingAllele, DifferentExpressingAllele);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithNullAlleleInStringAndExpressingAllele, 2);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        #endregion

        #region Single Allele vs. Allele String

        [TestCase(ExpressingAllele, NullAlleleInString1)]
        [TestCase(NullAlleleInString1, ExpressingAllele)]
        public async Task Search_WithOneAllowedMismatch_PatientWithTwoCopiesOfExpressingAllele_MatchesDonorWithSameExpressingAlleleAndNullAlleleInString(
            string donorHla1,
            string donorHla2)
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(donorHla1, donorHla2);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithTwoCopiesOfExpressingAllele, 1);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [TestCase(DifferentExpressingAllele, NullAlleleInString1)]
        [TestCase(NullAlleleInString1, DifferentExpressingAllele)]
        public async Task Search_WithTwoAllowedMismatches_PatientWithTwoCopiesOfExpressingAllele_MatchesDonorWithDifferentExpressingAlleleAndNullAlleleInString(
            string donorHla1,
            string donorHla2)
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(donorHla1, donorHla2);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithTwoCopiesOfExpressingAllele, 2);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        #endregion

        #region Allele String vs. Allele String

        [Test]
        public async Task Search_WithNoAllowedMismatches_PatientWithExpressingAlleleInStringAndNullAlleleInString_MatchesDonorWithSamePhenotype()
        {
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(patientWithExpressingAlleleInStringAndNullAlleleInString);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithExpressingAlleleInStringAndNullAlleleInString);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [TestCase(ExpressingAlleleInString2, DifferentNullAlleleInString)]
        [TestCase(DifferentNullAlleleInString, ExpressingAlleleInString2)]
        public async Task Search_WithOneAllowedMismatch_PatientWithExpressingAlleleInStringAndNullAlleleInString_MatchesDonorWithSameExpressingAlleleInStringButDifferentNullAlleleInString(
            string donorHla1,
            string donorHla2)
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(donorHla1, donorHla2);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithExpressingAlleleInStringAndNullAlleleInString, 1);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [TestCase(ExpressingAlleleInString2, ExpressingAlleleInString3)]
        [TestCase(ExpressingAlleleInString3, ExpressingAlleleInString2)]
        public async Task Search_WithOneAllowedMismatch_PatientWithExpressingAlleleInStringAndNullAlleleInString_MatchesDonorWithTwoCopiesOfSameExpressingAlleleInStrings(
            string donorHla1,
            string donorHla2)
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(donorHla1, donorHla2);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithExpressingAlleleInStringAndNullAlleleInString, 1);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [TestCase(NullAlleleInString2, DifferentExpressingAlleleInString)]
        [TestCase(DifferentExpressingAlleleInString, NullAlleleInString2)]
        public async Task Search_WithTwoAllowedMismatches_PatientWithExpressingAlleleInStringAndNullAlleleInString_MatchesDonorWithSameNullAlleleInStringButDifferentExpressingAlleleInString(
            string donorHla1,
            string donorHla2)
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(donorHla1, donorHla2);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithExpressingAlleleInStringAndNullAlleleInString, 2);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [TestCase(DifferentNullAlleleInString, DifferentExpressingAlleleInString)]
        [TestCase(DifferentExpressingAlleleInString, DifferentNullAlleleInString)]
        public async Task Search_WithTwoAllowedMismatches_PatientWithExpressingAlleleInStringAndNullAlleleInString_MatchesDonorWithDifferentNullAlleleAndDifferentExpressingAlleleInStrings(
            string donorHla1,
            string donorHla2)
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(donorHla1, donorHla2);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithExpressingAlleleInStringAndNullAlleleInString, 2);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatches_PatientWithExpressingAlleleInStringAndNullAlleleInString_MatchesDonorWithTwoCopiesOfDifferentExpressingAlleleInStrings()
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(DifferentExpressingAlleleInString, DifferentExpressingAlleleInString);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithExpressingAlleleInStringAndNullAlleleInString, 2);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_PatientWithTwoCopiesOfExpressingAlleleInStrings_MatchesDonorWithSamePhenotype()
        {
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(patientWithTwoCopiesOfExpressingAlleleInStrings);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithTwoCopiesOfExpressingAlleleInStrings);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [TestCase(ExpressingAlleleInString3, NullAlleleInString1)]
        [TestCase(NullAlleleInString1, ExpressingAlleleInString3)]
        public async Task Search_WithOneAllowedMismatch_PatientWithTwoCopiesOfExpressingAlleleInStrings_MatchesDonorWithSameExpressingAlleleInStringAndNullAlleleInString(
            string donorHla1,
            string donorHla2)
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(donorHla1, donorHla2);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithTwoCopiesOfExpressingAlleleInStrings, 1);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [TestCase(DifferentExpressingAlleleInString, NullAlleleInString1)]
        [TestCase(NullAlleleInString1, DifferentExpressingAlleleInString)]
        public async Task Search_WithTwoAllowedMismatches_PatientWithTwoCopiesOfExpressingAlleleInStrings_MatchesDonorWithDifferentExpressingAlleleInStringAndNullAlleleInString(
            string donorHla1,
            string donorHla2)
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(donorHla1, donorHla2);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithTwoCopiesOfExpressingAlleleInStrings, 2);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatches_PatientWithTwoCopiesOfExpressingAlleleInStrings_MatchesDonorWithTwoCopiesOfDifferentExpressingAlleleInStrings()
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(DifferentExpressingAlleleInString, DifferentExpressingAlleleInString);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithTwoCopiesOfExpressingAlleleInStrings, 2);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_PatientWithTwoNullAllelesInTwoStrings_MatchesDonorWithSamePhenotype()
        {
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(patientWithTwoNullAllelesInTwoStrings);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithTwoNullAllelesInTwoStrings);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [TestCase(ExpressingAlleleInString1, DifferentNullAlleleInString)]
        [TestCase(DifferentNullAlleleInString, ExpressingAlleleInString1)]
        public async Task Search_WithOneAllowedMismatch_PatientWithTwoNullAllelesInTwoStrings_MatchesDonorWithSameExpressingAlleleInStringButDifferentNullAlleleInString(
            string donorHla1,
            string donorHla2)
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(donorHla1, donorHla2);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithTwoNullAllelesInTwoStrings, 1);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [TestCase(ExpressingAlleleInString1, ExpressingAlleleInString2)]
        [TestCase(ExpressingAlleleInString2, ExpressingAlleleInString1)]
        public async Task Search_WithOneAllowedMismatch_PatientWithTwoNullAllelesInTwoStrings_MatchesDonorWithTwoCopiesOfExpressingAlleleInStrings(
            string donorHla1,
            string donorHla2)
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(donorHla1, donorHla2);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithTwoNullAllelesInTwoStrings, 1);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [TestCase(NullAlleleInString2, DifferentExpressingAlleleInString)]
        [TestCase(DifferentExpressingAlleleInString, NullAlleleInString2)]
        public async Task Search_WithTwoAllowedMismatches_PatientWithTwoNullAllelesInTwoStrings_MatchesDonorWithSameNullAlleleInStringButDifferentExpressingAlleleInString(
            string donorHla1,
            string donorHla2)
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(donorHla1, donorHla2);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithTwoNullAllelesInTwoStrings, 2);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [TestCase(DifferentNullAlleleInString, DifferentExpressingAlleleInString)]
        [TestCase(DifferentExpressingAlleleInString, DifferentNullAlleleInString)]
        public async Task Search_WithTwoAllowedMismatches_PatientWithTwoNullAllelesInTwoStrings_MatchesDonorWithDifferentNullAlleleInStringAndDifferentExpressingAlleleInString(
            string donorHla1,
            string donorHla2)
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(donorHla1, donorHla2);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithTwoNullAllelesInTwoStrings, 2);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [TestCase(NullAlleleInString2, NullRelatedToExpressingAlleleInString)]
        [TestCase(NullRelatedToExpressingAlleleInString, NullAlleleInString2)]
        public async Task Search_WithTwoAllowedMismatches_PatientWithTwoNullAllelesInTwoStrings_MatchesDonorWithSameNullAllelesButDifferentExpressingAllelesInStrings(
            string donorHla1,
            string donorHla2)
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(donorHla1, donorHla2);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithTwoNullAllelesInTwoStrings, 2);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatches_PatientWithTwoNullAllelesInTwoStrings_MatchesDonorWithTwoCopiesOfDifferentExpressingAlleleInStrings()
        {
            var donorPhenotype = BuildExpandedHlaPhenotype(DifferentExpressingAlleleInString, DifferentExpressingAlleleInString);
            var expectedDonorId = await AddSingleDonorPhenotypeToDonorRepository(donorPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithTwoNullAllelesInTwoStrings, 2);
            var actualDonorIds = await GetMatchingDonorIds(criteria);

            actualDonorIds.Should().Contain(expectedDonorId);
        }

        #endregion

        #region Helper Methods

        private void BuildPatientPhenotypes()
        {
            patientWithNullAlleleInStringAndExpressingAllele = BuildExpandedHlaPhenotype(NullAlleleInString1, ExpressingAllele);

            patientWithTwoCopiesOfExpressingAllele = BuildExpandedHlaPhenotype(ExpressingAllele, ExpressingAllele);

            patientWithExpressingAlleleInStringAndNullAlleleInString =
                BuildExpandedHlaPhenotype(ExpressingAlleleInString1, NullAlleleInString1);

            patientWithTwoCopiesOfExpressingAlleleInStrings =
                BuildExpandedHlaPhenotype(ExpressingAlleleInString1, ExpressingAlleleInString2);

            patientWithTwoNullAllelesInTwoStrings =
                BuildExpandedHlaPhenotype(NullAlleleInString1, ExpressingAlleleAndRelatedNullInString);
        }

        private PhenotypeInfo<ExpandedHla> BuildExpandedHlaPhenotype(string hla1, string hla2)
        {
            var newPhenotype = originalHlaPhenotype.Map((l, p, hla) => hla);
            newPhenotype.SetAtPosition(LocusUnderTest, TypePosition.One, hla1);
            newPhenotype.SetAtPosition(LocusUnderTest, TypePosition.Two, hla2);

            return expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(newPhenotype).Result;
        }

        private async Task<int> AddSingleDonorPhenotypeToDonorRepository(PhenotypeInfo<ExpandedHla> donorPhenotype)
        {
            var donor = new InputDonorWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(donorPhenotype)
                .Build();

            await donorImportRepository.InsertDonorWithExpandedHla(donor);

            return donor.DonorId;
        }

        /// <summary>
        /// Runs the matching service based on match criteria.
        /// </summary>
        private async Task<IEnumerable<int>> GetMatchingDonorIds(AlleleLevelMatchCriteria alleleLevelMatchCriteria)
        {
            var results = await donorMatchingService.GetMatches(alleleLevelMatchCriteria);
            return results.Select(d => d.DonorId);
        }

        #endregion
    }
}
