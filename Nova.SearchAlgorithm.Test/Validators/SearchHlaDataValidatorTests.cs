using FluentValidation.TestHelper;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Validators;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Client.Validators
{
    [TestFixture]
    public class LocusSearchHlaDataValidatorTests
    {
        private SearchHlaDataValidator validator;
        
        [SetUp]
        public void SetUp()
        {
            validator = new SearchHlaDataValidator();
        }
        
        [Test]
        public void Validator_WhenMissingLocusMismatchCriteriaA_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.LocusSearchHlaA, (LocusSearchHla) null);
        }

        [Test]
        public void Validator_WhenMissingLocusMismatchCriteriaB_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.LocusSearchHlaB, (LocusSearchHla) null);
        }

        [Test]
        public void Validator_WhenMissingLocusMismatchCriteriaDrb1_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.LocusSearchHlaDrb1, (LocusSearchHla) null);
        }
        
        [Test]
        public void Validator_WhenMissingLocusMismatchCriteriaDqb1_ShouldNotHaveValidationError()
        {
            validator.ShouldNotHaveValidationErrorFor(x => x.LocusSearchHlaDqb1, (LocusSearchHla) null);
        }
        
        [Test]
        public void Validator_WhenMissingLocusMismatchCriteriaC_ShouldNotHaveValidationError()
        {
            validator.ShouldNotHaveValidationErrorFor(x => x.LocusSearchHlaC, (LocusSearchHla) null);
        }
    }
}