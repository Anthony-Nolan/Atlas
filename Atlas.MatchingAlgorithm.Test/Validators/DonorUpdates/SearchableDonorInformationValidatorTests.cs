using Atlas.MatchingAlgorithm.Validators.DonorInfo;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validators.DonorUpdates
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
    }
}