using Autofac;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Test.Integration.TestData;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;

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

        private PhenotypeInfo<ExpandedHla> patientWithNullAlleleInStringPhenotype;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            originalHlaPhenotype = new TestHla.HeterozygousSet1().FiveLocus_SingleExpressingAlleles;
            criteriaFromExpandedHla = new AlleleLevelMatchCriteriaFromExpandedHla(LocusUnderTest, MatchingDonorType);
            expandHlaPhenotypeService = Container.Resolve<IExpandHlaPhenotypeService>();
            donorImportRepository = Container.Resolve<IDonorImportRepository>();

            BuildPatientPhenotypes();
        }

        [SetUp]
        public void SetUpBeforeEachTest()
        {
            donorMatchingService = Container.Resolve<IDonorMatchingService>();
            ResetDatabase();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ClearDatabase();
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_PatientWithNullAlleleInString_MatchesDonorWithSamePhenotype()
        {
            var expectedDonorId = await PopulateDonorRepositoryWithSingleDonorPhenotype(patientWithNullAlleleInStringPhenotype);

            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientWithNullAlleleInStringPhenotype);
            var actualDonorId = await GetMatchingDonorId(criteria);

            actualDonorId.Should().Be(expectedDonorId);
        }

        private async Task<int> PopulateDonorRepositoryWithSingleDonorPhenotype(PhenotypeInfo<ExpandedHla> donorPhenotype)
        {
            ResetDatabase();

            var donor = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(donorPhenotype)
                .Build();

            await donorImportRepository.AddOrUpdateDonorWithHla(donor);

            return donor.DonorId;
        }

        private void BuildPatientPhenotypes()
        {
            patientWithNullAlleleInStringPhenotype = BuildExpandedHlaPhenotype(NullAlleleInString1, ExpressingAllele);
        }

        private PhenotypeInfo<ExpandedHla> BuildExpandedHlaPhenotype(string hla1, string hla2)
        {
            var newPhenotype = originalHlaPhenotype.Map((l, p, hla) => hla);
            newPhenotype.SetAtPosition(LocusUnderTest, TypePositions.One, hla1);
            newPhenotype.SetAtPosition(LocusUnderTest, TypePositions.Two, hla2);

            return expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(newPhenotype).Result;
        }

        /// <summary>
        /// Runs the matching service based on match criteria.
        /// </summary>
        /// <returns>List of matching donor IDs.</returns>
        private async Task<int> GetMatchingDonorId(AlleleLevelMatchCriteria alleleLevelMatchCriteria)
        {
            var results = await donorMatchingService.GetMatches(alleleLevelMatchCriteria);
            return results
                .Select(d => d.DonorId)
                .SingleOrDefault();
        }
    }
}
