using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validators.SearchRequest
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

        [TestCase(-1)]
        [TestCase(5)]
        public void Validator_WhenDonorMismatchCountInvalid_ShouldHaveValidationError(int totalMismatchCount)
        {
            var mismatchCriteria = new MismatchCriteria
            {
                LocusMismatchCounts = new LociInfo<int?>(0),
                DonorMismatchCount = totalMismatchCount
            };

            validator.Invoking(v => v.ValidateAndThrow(mismatchCriteria)).Should().Throw<ValidationException>();
        }

        [Test]
        public void Validator_WhenNoLocusMismatchCriteriaProvided_ShouldHaveValidationError()
        {
            var mismatchCriteria = new MismatchCriteria
            {
                LocusMismatchCounts = null,
            };

            validator.Invoking(v => v.ValidateAndThrow(mismatchCriteria)).Should().Throw<ValidationException>();
        }

        [TestCase(Locus.A)]
        [TestCase(Locus.B)]
        [TestCase(Locus.Drb1)]
        public void Validator_WhenMissingRequiredLocusMismatchCriteria_ShouldHaveValidationError(Locus locus)
        {
            var mismatchCriteria = new MismatchCriteria {LocusMismatchCounts = new LociInfo<int?>(0)};
            mismatchCriteria.LocusMismatchCounts.SetLocus(locus, null);

            validator.Invoking(v => v.ValidateAndThrow(mismatchCriteria)).Should().Throw<ValidationException>();
        }

        [TestCase(Locus.A, -1)]
        [TestCase(Locus.A, 5)]
        [TestCase(Locus.B, -1)]
        [TestCase(Locus.B, 5)]
        [TestCase(Locus.Drb1, -1)]
        [TestCase(Locus.Drb1, 5)]
        [TestCase(Locus.C, -1)]
        [TestCase(Locus.C, 5)]
        [TestCase(Locus.Dqb1, -1)]
        [TestCase(Locus.Dqb1, 5)]
        public void Validator_WhenLocusMismatchCountInvalid_ShouldHaveValidationError(Locus locus, int mismatchCount)
        {
            var mismatchCriteria = new MismatchCriteria {LocusMismatchCounts = new LociInfo<int?>(0)};
            mismatchCriteria.LocusMismatchCounts.SetLocus(locus, mismatchCount);

            validator.Invoking(v => v.ValidateAndThrow(mismatchCriteria)).Should().Throw<ValidationException>();
        }
    }
}