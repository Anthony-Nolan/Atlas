using FluentAssertions;
using FluentValidation.TestHelper;
using Nova.SearchAlgorithm.Client.Models;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Client
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

        [Test]
        public void Validator_WhenNoHlaStringsAreProvided_ShouldHaveValidationError()
        {
            var locusMismatchCriteria = new LocusMismatchCriteria
            {
                MismatchCount = 0
            };
            var result = validator.Validate(locusMismatchCriteria);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenEmptyHlaStringsAreProvided_ShouldHaveValidationError()
        {
            var locusMismatchCriteria = new LocusMismatchCriteria
            {
                MismatchCount = 0,
                SearchHla1 = "",
                SearchHla2 = ""
            };
            var result = validator.Validate(locusMismatchCriteria);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenOneHlaStringProvided_ShouldHaveValidationError()
        {
            var locusMismatchCriteria = new LocusMismatchCriteria
            {
                MismatchCount = 0,
                SearchHla2 = "hla-string"
            };
            var result = validator.Validate(locusMismatchCriteria);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenBothHlaStringsProvided_ShouldNotHaveValidationError()
        {
            var locusMismatchCriteria = new LocusMismatchCriteria
            {
                MismatchCount = 0,
                SearchHla1 = "hla-string",
                SearchHla2 = "hla-string-2"
            };
            var result = validator.Validate(locusMismatchCriteria);
            result.IsValid.Should().BeTrue();
        }
    }
}
