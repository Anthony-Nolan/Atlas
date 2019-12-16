using FluentValidation.TestHelper;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Validators.DonorInfo;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Validators.DonorUpdates
{
    [TestFixture]
    public class SearchableDonorInformationValidatorTests
    {
        private SearchableDonorInformationValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new SearchableDonorInformationValidator();
        }

        [TestCase("")]
        [TestCase(null)]
        public void Validator_WhenDonorTypeMissing_ShouldHaveValidationError(string missingString)
        {
            validator.ShouldHaveValidationErrorFor(x => x.DonorType, missingString);
        }

        [TestCase("0")]
        [TestCase("donor-type")]
        public void Validator_WhenDonorTypeIsNotValid_ShouldHaveValidationError(string donorType)
        {
            validator.ShouldHaveValidationErrorFor(x => x.DonorType, donorType);
        }

        [TestCase("A")]
        [TestCase("Adult")]
        [TestCase("C")]
        [TestCase("Cord")]
        public void Validator_WhenDonorTypeIsValid_ShouldNotHaveValidationError(string donorType)
        {
            validator.ShouldNotHaveValidationErrorFor(x => x.DonorType, donorType);
        }

        [TestCase("")]
        [TestCase(null)]
        public void Validator_WhenRegistryCodeMissing_ShouldHaveValidationError(string missingString)
        {
            validator.ShouldHaveValidationErrorFor(x => x.RegistryCode, missingString);
        }

        [TestCase("0")]
        [TestCase("an")]
        [TestCase("registry-code")]
        public void Validator_WhenRegistryCodeNotEnumName_ShouldHaveValidationError(string invalidName)
        {
            validator.ShouldHaveValidationErrorFor(x => x.RegistryCode, invalidName);
        }

        [Test]
        public void Validator_WhenRegistryCodeIsEnumName_ShouldNotHaveValidationError()
        {
            validator.ShouldNotHaveValidationErrorFor(x => x.RegistryCode, $"{RegistryCode.AN}");
        }
    }
}