using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Validators.DonorInfo;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validators.DonorInfo
{
    [TestFixture]
    public class RequiredLocusHlaNamesValidatorTests
    {
        private RequiredLocusHlaNamesValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new RequiredLocusHlaNamesValidator();
        }

        [Test]
        public void Validator_WhenNoHlaStringsAreProvided_ShouldHaveValidationError()
        {
            var locusHlaNames = new LocusInfo<string>();
            var result = validator.Validate(locusHlaNames);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenEmptyHlaStringsAreProvided_ShouldHaveValidationError()
        {
            var locusHlaNames = new LocusInfo<string>("");
            var result = validator.Validate(locusHlaNames);
            result.IsValid.Should().BeFalse();
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
            var locusHlaNames = new LocusInfo<string>("hla-string-1", "hla-string-2");
            var result = validator.Validate(locusHlaNames);
            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validator_WhenFirstHlaStringNull_ShouldHaveValidationError()
        {
            var locusHlaNames = new LocusInfo<string>(null, "not-null");
            var result = validator.Validate(locusHlaNames);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenSecondHlaStringNull_ShouldHaveValidationError()
        {
            var locusHlaNames = new LocusInfo<string>("not-null", null);
            var result = validator.Validate(locusHlaNames);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenBothHlaStringsNull_ShouldHaveValidationError()
        {
            var locusHlaNames = new LocusInfo<string>(null as string);
            var result = validator.Validate(locusHlaNames);
            result.IsValid.Should().BeFalse();
        }
    }
}