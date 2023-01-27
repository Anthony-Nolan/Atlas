using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Validators.DonorInfo;
using FluentAssertions;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validators.DonorUpdates
{
    [TestFixture]
    public class SearchableDonorUpdateValidatorTests
    {
        private const int ValidDonorId = 123;
        private SearchableDonorUpdateValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new SearchableDonorUpdateValidator();
        }

        [Test]
        public void Validator_WhenDonorIdCanBeParsedToInteger_ShouldNotHaveValidationError()
        {
            validator.ShouldNotHaveValidationErrorFor(x => x.DonorId, ValidDonorId);
        }

        [Test]
        public void Validator_WhenDonorAvailableForSearch_AndSearchableDonorInformationIsMissing_ShouldHaveValidationError()
        {
            var update = new SearchableDonorUpdate
            {
                DonorId = ValidDonorId,
                IsAvailableForSearch = true,
                SearchableDonorInformation = null
            };

            var result = validator.Validate(update);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenDonorUnavailableForSearch_AndSearchableDonorInformationIsMissing_ShouldNotHaveValidationError()
        {
            var update = new SearchableDonorUpdate
            {
                DonorId = ValidDonorId,
                IsAvailableForSearch = false,
                SearchableDonorInformation = null
            };

            var result = validator.Validate(update);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validator_WhenTopLevelDonorIdDiffersToNestedDonorId_ShouldHaveValidationError()
        {
            const string hlaName = "hla-name";

            var update = new SearchableDonorUpdate
            {
                DonorId = ValidDonorId + 1,
                SearchableDonorInformation = new SearchableDonorInformation
                {
                    DonorId = ValidDonorId,
                    DonorType = DonorType.Adult,
                    A_1 = hlaName,
                    A_2 = hlaName,
                    B_1 = hlaName,
                    B_2 = hlaName,
                    DRB1_1 = hlaName,
                    DRB1_2 = hlaName
                }
            };

            var result = validator.Validate(update);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenTopLevelDonorIdEqualsNestedDonorId_ShouldNotHaveValidationError()
        {
            const string hlaName = "hla-name";

            var update = new SearchableDonorUpdate
            {
                DonorId = ValidDonorId,
                SearchableDonorInformation = new SearchableDonorInformation
                {
                    DonorId = ValidDonorId,
                    DonorType = DonorType.Adult,
                    A_1 = hlaName,
                    A_2 = hlaName,
                    B_1 = hlaName,
                    B_2 = hlaName,
                    DRB1_1 = hlaName,
                    DRB1_2 = hlaName
                }
            };

            var result = validator.Validate(update);

            result.IsValid.Should().BeTrue();
        }
    }
}