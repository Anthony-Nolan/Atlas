using Atlas.Client.Models.Search.Requests;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using FluentAssertions;
using FluentValidation;
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
        // TODO ATLAS-865
        [TestCase(6)]
        public void Validator_WhenDonorMismatchCountInvalid_ShouldHaveValidationError(int totalMismatchCount)
        {
            var locusCriteria = new LociInfoBuilder<int?>(0)
                .WithDataAt(Locus.Dpb1, null)
                .Build()
                .ToLociInfoTransfer();

            var mismatchCriteria = new MismatchCriteria
            {
                LocusMismatchCriteria = locusCriteria,
                DonorMismatchCount = totalMismatchCount
            };

            validator.Invoking(v => v.ValidateAndThrow(mismatchCriteria)).Should().Throw<ValidationException>();
        }

        [Test]
        public void Validator_WhenNoLocusMismatchCriteriaProvided_ShouldHaveValidationError()
        {
            var mismatchCriteria = new MismatchCriteria
            {
                LocusMismatchCriteria = null,
            };

            validator.Invoking(v => v.ValidateAndThrow(mismatchCriteria)).Should().Throw<ValidationException>();
        }

        [TestCase(Locus.A)]
        [TestCase(Locus.B)]
        [TestCase(Locus.Drb1)]
        public void Validator_WhenMissingRequiredLocusMismatchCriteria_ShouldHaveValidationError(Locus testLocus)
        {
            var locusCriteria = new LociInfoBuilder<int?>(0)
                .WithDataAt(Locus.Dpb1, null)
                .WithDataAt(testLocus, null)
                .Build()
                .ToLociInfoTransfer();

            var mismatchCriteria = new MismatchCriteria
            {
                LocusMismatchCriteria = locusCriteria
            };

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
        public void Validator_WhenLocusMismatchCountInvalid_ShouldHaveValidationError(Locus testLocus, int mismatchCount)
        {
            var locusCriteria = new LociInfoBuilder<int?>(0)
                .WithDataAt(Locus.Dpb1, null)
                .WithDataAt(testLocus, mismatchCount)
                .Build()
                .ToLociInfoTransfer();

            var mismatchCriteria = new MismatchCriteria
            {
                LocusMismatchCriteria = locusCriteria
            };

            validator.Invoking(v => v.ValidateAndThrow(mismatchCriteria)).Should().Throw<ValidationException>();
        }
    }
}