using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Scoring.Grading
{
    [TestFixture]
    public class PermissiveMismatchCalculatorTests
    {
        private const Locus Dpb1Locus = Locus.Dpb1;
        private const Locus NonDpb1Locus = Locus.A;
        private const string PatientHlaName = "patient-hla-name";
        private const string DonorHlaName = "donor-hla-name";
        private const string NoTceGroupAssignment = "";

        private IDpb1TceGroupMetadataService dpb1TceGroupMetadataService;
        private IPermissiveMismatchCalculator permissiveMismatchCalculator;

        [SetUp]
        public void SetUpBeforeEachTest()
        {
            dpb1TceGroupMetadataService = Substitute.For<IDpb1TceGroupMetadataService>();

            var hlaMetadataDictionaryBuilder = new HlaMetadataDictionaryBuilder().Using(dpb1TceGroupMetadataService);
            var hlaVersionAccessor = Substitute.For<IActiveHlaNomenclatureVersionAccessor>();

            permissiveMismatchCalculator = new PermissiveMismatchCalculator(hlaMetadataDictionaryBuilder, hlaVersionAccessor);
        }

        #region Tests: Non-DPB1 Locus

        [Test]
        public void IsPermissiveMismatch_AtNonDpb1Locus_ReturnsFalse()
        {
            var actualResult = permissiveMismatchCalculator.IsPermissiveMismatch(NonDpb1Locus, PatientHlaName, DonorHlaName);

            actualResult.Should().BeFalse();
        }

        #endregion

        #region Tests: DPB1

        [Test]
        public void IsPermissiveMismatch_AtDpb1Locus_PatientAndDonorHaveSameTceGroup_ReturnsTrue()
        {
            const string sharedTceGroup = "shared-tce-group";
            dpb1TceGroupMetadataService.GetDpb1TceGroup(Arg.Any<string>(), Arg.Any<string>()).Returns(sharedTceGroup);

            var actualResult = permissiveMismatchCalculator.IsPermissiveMismatch(Dpb1Locus, PatientHlaName, DonorHlaName);

            actualResult.Should().BeTrue();
        }

        [Test]
        public void IsPermissiveMismatch_AtDpb1Locus_PatientAndDonorHaveDifferentTceGroup_ReturnsFalse()
        {
            const string patientTceGroup = "patient-tce-group";
            dpb1TceGroupMetadataService.GetDpb1TceGroup(PatientHlaName, Arg.Any<string>()).Returns(patientTceGroup);

            const string donorTceGroup = "donor-tce-group";
            dpb1TceGroupMetadataService.GetDpb1TceGroup(DonorHlaName, Arg.Any<string>()).Returns(donorTceGroup);

            var actualResult = permissiveMismatchCalculator.IsPermissiveMismatch(Dpb1Locus, PatientHlaName, DonorHlaName);

            actualResult.Should().BeFalse();
        }

        [Test]
        public void IsPermissiveMismatch_AtDpb1Locus_PatientHasNoTceGroupAssigned_AndDonorHasTceGroup_ReturnsFalse()
        {
            dpb1TceGroupMetadataService.GetDpb1TceGroup(PatientHlaName, Arg.Any<string>()).Returns(NoTceGroupAssignment);

            const string donorTceGroup = "donor-tce-group";
            dpb1TceGroupMetadataService.GetDpb1TceGroup(DonorHlaName, Arg.Any<string>()).Returns(donorTceGroup);

            var actualResult = permissiveMismatchCalculator.IsPermissiveMismatch(Dpb1Locus, PatientHlaName, DonorHlaName);

            actualResult.Should().BeFalse();
        }

        [Test]
        public void IsPermissiveMismatch_AtDpb1Locus_PatientHasTceGroup_AndDonorHasNoTceGroupAssigned_ReturnsFalse()
        {
            const string patientTceGroup = "patient-tce-group";
            dpb1TceGroupMetadataService.GetDpb1TceGroup(PatientHlaName, Arg.Any<string>()).Returns(patientTceGroup);

            dpb1TceGroupMetadataService.GetDpb1TceGroup(DonorHlaName, Arg.Any<string>()).Returns(NoTceGroupAssignment);

            var actualResult = permissiveMismatchCalculator.IsPermissiveMismatch(Dpb1Locus, PatientHlaName, DonorHlaName);

            actualResult.Should().BeFalse();
        }

        [Test]
        public void IsPermissiveMismatch_AtDpb1Locus_PatientAndDonorHaveNoTceGroupsAssigned_ReturnsFalse()
        {
            dpb1TceGroupMetadataService.GetDpb1TceGroup(PatientHlaName, Arg.Any<string>()).Returns(NoTceGroupAssignment);
            dpb1TceGroupMetadataService.GetDpb1TceGroup(DonorHlaName, Arg.Any<string>()).Returns(NoTceGroupAssignment);

            var actualResult = permissiveMismatchCalculator.IsPermissiveMismatch(Dpb1Locus, PatientHlaName, DonorHlaName);

            actualResult.Should().BeFalse();
        }

        #endregion
    }
}