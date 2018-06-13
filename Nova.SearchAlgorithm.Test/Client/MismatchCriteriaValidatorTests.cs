using FluentValidation.TestHelper;
using Nova.SearchAlgorithm.Client.Models;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Client
{
    [TestFixture]
    public class MismatchCriteriaValidatorTests
    {
        private MismatchCriteriaValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new MismatchCriteriaValidator();
        }

        [Test]
        public void Validator_WhenMissingDonorMismatchCount_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.DonorMismatchCount, (int?) null);
        }

        [Test]
        public void Validator_WhenDonorMismatchCountLessThanZero_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.DonorMismatchCount, -1);
        }

        [Test]
        public void Validator_WhenDonorMismatchCountGreaterThanTwo_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.DonorMismatchCount, 3);
        }

        [Test]
        public void Validator_WhenMissingLocusMismatchCriteriaA_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.LocusMismatchA, (LocusMismatchCriteria) null);
        }

        [Test]
        public void Validator_WhenMissingLocusMismatchCriteriaB_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.LocusMismatchB, (LocusMismatchCriteria) null);
        }

        [Test]
        public void Validator_WhenMissingLocusMismatchCriteriaDrb1_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.LocusMismatchDRB1, (LocusMismatchCriteria) null);
        }
    }
}
