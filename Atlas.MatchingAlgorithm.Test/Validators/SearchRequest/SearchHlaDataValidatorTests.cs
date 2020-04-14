using FluentValidation.TestHelper;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validators.SearchRequest
{
    [TestFixture]
    public class SearchHlaDataValidatorTests
    {
        private SearchHlaDataValidator validator;
        
        [SetUp]
        public void SetUp()
        {
            validator = new SearchHlaDataValidator();
        }
        
        [Test]
        public void Validator_WhenMissingLocusSearchHlaA_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.LocusSearchHlaA, (LocusSearchHla) null);
        }

        [Test]
        public void Validator_WhenMissingLocusSearchHlaB_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.LocusSearchHlaB, (LocusSearchHla) null);
        }

        [Test]
        public void Validator_WhenMissingLocusSearchHlaDrb1_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.LocusSearchHlaDrb1, (LocusSearchHla) null);
        }

        [Test]
        public void Validator_WhenMissingLocusSearchHlaC_ShouldNotHaveValidationError()
        {
            validator.ShouldNotHaveValidationErrorFor(x => x.LocusSearchHlaC, (LocusSearchHla) null);
        }

        [Test]
        public void Validator_WhenMissingLocusSearchHlaDqb1_ShouldNotHaveValidationError()
        {
            validator.ShouldNotHaveValidationErrorFor(x => x.LocusSearchHlaDqb1, (LocusSearchHla)null);
        }

        [Test]
        public void Validator_WhenMissingLocusSearchHlaDpb1_ShouldNotHaveValidationError()
        {
            validator.ShouldNotHaveValidationErrorFor(x => x.LocusSearchHlaDpb1, (LocusSearchHla)null);
        }
    }
}