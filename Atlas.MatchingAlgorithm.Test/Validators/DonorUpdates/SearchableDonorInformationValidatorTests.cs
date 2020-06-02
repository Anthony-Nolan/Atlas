using Atlas.MatchingAlgorithm.Client.Models.Donors;
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

        [TestCase(DonorType.Adult)]
        [TestCase(DonorType.Cord)]
        public void Validator_WhenDonorTypeIsValid_ShouldNotHaveValidationError(DonorType donorType)
        {
            validator.ShouldNotHaveValidationErrorFor(x => x.DonorType, donorType);
        }
    }
}