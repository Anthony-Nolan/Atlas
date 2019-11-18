using FluentValidation.TestHelper;
using Nova.SearchAlgorithm.Validators.SearchRequest;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Validators.SearchRequest
{
    [TestFixture]
    public class LocusMismatchCriteriaValidatorTests
    {
        private LocusMismatchCriteriaValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new LocusMismatchCriteriaValidator();
        }
        
        [Test]
        public void Validator_WhenMismatchCountLessThanZero_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.MismatchCount, -1);
        }

        [Test]
        public void Validator_WhenMismatchCountGreaterThanFour_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.MismatchCount, 3);
        }
    }
}
