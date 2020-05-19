using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validators.SearchRequest
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
