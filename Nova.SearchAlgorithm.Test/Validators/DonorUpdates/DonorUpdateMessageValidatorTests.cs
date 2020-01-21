using FluentValidation.TestHelper;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.Validators.DonorInfo;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Validators.DonorUpdates
{
    [TestFixture]
    public class DonorUpdateMessageValidatorTests
    {
        private DonorUpdateMessageValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new DonorUpdateMessageValidator();
        }

        [TestCase("")]
        [TestCase(null)]
        public void Validator_WhenLockTokenIsMissing_ShouldHaveValidationError(string missingString)
        {
            validator.ShouldHaveValidationErrorFor(x => x.LockToken, missingString);
        }

        [Test]
        public void Validator_WhenDeserializedBodyIsNull_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.DeserializedBody, (SearchableDonorUpdate)null);
        }
    }
}