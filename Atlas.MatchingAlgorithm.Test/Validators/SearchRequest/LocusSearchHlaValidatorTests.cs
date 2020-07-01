using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validators.SearchRequest
{
    [TestFixture]
    public class LocusSearchHlaValidatorTests
    {
        private LocusSearchHlaValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new LocusSearchHlaValidator();
        }

        [Test]
        public void Validator_WhenNoHlaStringsAreProvided_ShouldHaveValidationError()
        {
            var searchHla = new LocusInfo<string>();
            var result = validator.Validate(searchHla);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenEmptyHlaStringsAreProvided_ShouldHaveValidationError()
        {
            var searchHla = new LocusInfo<string>
            {
                Position1 = "",
                Position2 = ""
            };
            var result = validator.Validate(searchHla);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenOnlyFirstHlaStringProvided_ShouldHaveValidationError()
        {
            var searchHla = new LocusInfo<string>
            {
                Position1 = "hla-string"
            };
            var result = validator.Validate(searchHla);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenOnlySecondHlaStringProvided_ShouldHaveValidationError()
        {
            var searchHla = new LocusInfo<string>
            {
                Position2 = "hla-string"
            };
            var result = validator.Validate(searchHla);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenBothHlaStringsProvided_ShouldNotHaveValidationError()
        {
            var searchHla = new LocusInfo<string>
            {
                Position1 = "hla-string",
                Position2 = "hla-string-2"
            };
            var result = validator.Validate(searchHla);
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validator_WhenFirstHlaStringNull_ShouldHaveValidationError()
        {
            var searchHla = new LocusInfo<string>
            {
                Position1 = null,
                Position2 = "not-null"
            };
            var result = validator.Validate(searchHla);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenSecondHlaStringNull_ShouldHaveValidationError()
        {
            var searchHla = new LocusInfo<string>
            {
                Position1 = "not-null",
                Position2 = null
            };
            var result = validator.Validate(searchHla);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenBothHlaStringsNull_ShouldHaveValidationError()
        {
            var searchHla = new LocusInfo<string>
            {
                Position1 = null,
                Position2 = null
            };
            var result = validator.Validate(searchHla);
            result.IsValid.Should().BeFalse();
        }
    }
}