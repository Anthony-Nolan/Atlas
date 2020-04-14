using FluentAssertions;
using Atlas.MatchingAlgorithm.Validators.DonorInfo;
using Nova.Utils.PhenotypeInfo;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validators.DonorInfo
{
    [TestFixture]
    public class OptionalLocusHlaNamesValidatorTests
    {
        private OptionalLocusHlaNamesValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new OptionalLocusHlaNamesValidator();
        }

        [Test]
        public void Validator_WhenNoHlaStringsAreProvided_ShouldHaveValidationError()
        {
            var locusHlaNames = new LocusInfo<string>();
            var result = validator.Validate(locusHlaNames);
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validator_WhenEmptyHlaStringsAreProvided_ShouldNotHaveValidationError()
        {
            var locusHlaNames = new LocusInfo<string>
            {
                Position1 = "",
                Position2 = ""
            };
            var result = validator.Validate(locusHlaNames);
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validator_WhenOnlyFirstHlaStringProvided_ShouldHaveValidationError()
        {
            var locusHlaNames = new LocusInfo<string>
            {
                Position1 = "hla-string"
            };
            var result = validator.Validate(locusHlaNames);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenOnlySecondHlaStringProvided_ShouldHaveValidationError()
        {
            var locusHlaNames = new LocusInfo<string>
            {
                Position2 = "hla-string"
            };
            var result = validator.Validate(locusHlaNames);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenBothHlaStringsProvided_ShouldNotHaveValidationError()
        {
            var locusHlaNames = new LocusInfo<string>
            {
                Position1 = "hla-string-1",
                Position2 = "hla-string-2"
            };
            var result = validator.Validate(locusHlaNames);
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validator_WhenFirstHlaStringNull_ShouldHaveValidationError()
        {
            var locusHlaNames = new LocusInfo<string>
            {
                Position1 = null,
                Position2 = "not-null"
            };
            var result = validator.Validate(locusHlaNames);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenSecondHlaStringNull_ShouldHaveValidationError()
        {
            var locusHlaNames = new LocusInfo<string>
            {
                Position1 = "not-null",
                Position2 = null
            };
            var result = validator.Validate(locusHlaNames);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenBothHlaStringsNull_ShouldNotHaveValidationError()
        {
            var locusHlaNames = new LocusInfo<string>
            {
                Position1 = null,
                Position2 = null
            };
            var result = validator.Validate(locusHlaNames);
            result.IsValid.Should().BeTrue();
        }
    }
}