using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using FluentValidation.TestHelper;
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
            validator.ShouldHaveValidationErrorFor(x => x.A, (LocusInfo<string>) null);
        }

        [Test]
        public void Validator_WhenMissingLocusSearchHlaB_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.B, (LocusInfo<string>) null);
        }

        [Test]
        public void Validator_WhenMissingLocusSearchHlaDrb1_ShouldHaveValidationError()
        {
            validator.ShouldHaveValidationErrorFor(x => x.Drb1, (LocusInfo<string>) null);
        }

        [Test]
        public void Validator_WhenMissingLocusSearchHlaC_ShouldNotHaveValidationError()
        {
            validator.ShouldNotHaveValidationErrorFor(x => x.C, (LocusInfo<string>) null);
        }

        [Test]
        public void Validator_WhenMissingLocusSearchHlaDqb1_ShouldNotHaveValidationError()
        {
            validator.ShouldNotHaveValidationErrorFor(x => x.Dqb1, (LocusInfo<string>) null);
        }

        [Test]
        public void Validator_WhenMissingLocusSearchHlaDpb1_ShouldNotHaveValidationError()
        {
            validator.ShouldNotHaveValidationErrorFor(x => x.Dpb1, (LocusInfo<string>) null);
        }
    }
}