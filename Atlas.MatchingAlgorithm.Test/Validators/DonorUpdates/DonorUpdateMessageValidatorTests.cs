using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Validators.DonorInfo;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validators.DonorUpdates
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