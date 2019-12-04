using FluentAssertions;
using FluentValidation.TestHelper;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.DonorService.Client.Models.SearchableDonors;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Validators.DonorUpdates;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Validators.DonorUpdates
{
    [TestFixture]
    public class SearchableDonorUpdateValidatorTests
    {
        private const int ValidDonorId = 123;
        private static readonly string ValidDonorIdAsString = $"{ValidDonorId}";
        private SearchableDonorUpdateValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new SearchableDonorUpdateValidator();
        }

        [TestCase("")]
        [TestCase(null)]
        public void Validator_WhenDonorIdIsMissing_ShouldHaveValidationError(string missingString)
        {
            validator.ShouldHaveValidationErrorFor(x => x.DonorId, missingString);
        }

        [Test]
        public void Validator_WhenDonorIdCannotBeParsedToInteger_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.DonorId, "abc123");
        }

        [Test]
        public void Validator_WhenDonorIdCanBeParsedToInteger_ShouldNotHaveValidationError()
        {
            validator.ShouldNotHaveValidationErrorFor(x => x.DonorId, ValidDonorIdAsString);
        }

        [Test]
        public void Validator_WhenDonorAvailableForSearch_AndSearchableDonorInformationIsMissing_ShouldHaveValidationError()
        {
            var update = new SearchableDonorUpdateModel
            {
                DonorId = ValidDonorIdAsString,
                IsAvailableForSearch = true,
                SearchableDonorInformation = null
            };

            var result = validator.Validate(update);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenDonorUnavailableForSearch_AndSearchableDonorInformationIsMissing_ShouldNotHaveValidationError()
        {
            var update = new SearchableDonorUpdateModel
            {
                DonorId = ValidDonorIdAsString,
                IsAvailableForSearch = false,
                SearchableDonorInformation = null
            };

            var result = validator.Validate(update);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validator_WhenTopLevelDonorIdDiffersToNestedDonorId_ShouldHaveValidationError()
        {
            var update = new SearchableDonorUpdateModel
            {
                DonorId = $"{ValidDonorId + 1}",
                SearchableDonorInformation = new SearchableDonorInformation
                {
                    DonorId = ValidDonorId,
                    DonorType = $"{DonorType.Adult}",
                    RegistryCode = $"{RegistryCode.AN}"
                }
            };

            var result = validator.Validate(update);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenTopLevelDonorIdEqualsNestedDonorId_ShouldNotHaveValidationError()
        {
            var update = new SearchableDonorUpdateModel
            {
                DonorId = ValidDonorIdAsString,
                SearchableDonorInformation = new SearchableDonorInformation
                {
                    DonorId = ValidDonorId,
                    DonorType = $"{DonorType.Adult}",
                    RegistryCode = $"{RegistryCode.AN}"
                }
            };

            var result = validator.Validate(update);

            result.IsValid.Should().BeTrue();
        }
    }
}