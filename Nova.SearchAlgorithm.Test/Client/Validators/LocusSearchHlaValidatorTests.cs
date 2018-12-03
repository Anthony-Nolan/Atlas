using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Client.Validators
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
            var searchHla = new LocusSearchHla();
            var result = validator.Validate(searchHla);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenEmptyHlaStringsAreProvided_ShouldHaveValidationError()
        {
            var searchHla = new LocusSearchHla
            {
                SearchHla1 = "",
                SearchHla2 = ""
            };
            var result = validator.Validate(searchHla);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenOnlyFirstHlaStringProvided_ShouldHaveValidationError()
        {
            var searchHla = new LocusSearchHla
            {
                SearchHla1 = "hla-string"
            };
            var result = validator.Validate(searchHla);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenOnlySecondHlaStringProvided_ShouldHaveValidationError()
        {
            var searchHla = new LocusSearchHla
            {
                SearchHla2 = "hla-string"
            };
            var result = validator.Validate(searchHla);
            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validator_WhenBothHlaStringsProvided_ShouldNotHaveValidationError()
        {
            var searchHla = new LocusSearchHla
            {
                SearchHla1 = "hla-string",
                SearchHla2 = "hla-string-2"
            };
            var result = validator.Validate(searchHla);
            result.IsValid.Should().BeTrue();
        }
        
        [Test]
        public void Validator_WhenFirstHlaStringNull_ShouldHaveValidationError()
        {
            var searchHla = new LocusSearchHla
            {
                SearchHla1 = null,
                SearchHla2 = "not-null"
            };
            var result = validator.Validate(searchHla);
            result.IsValid.Should().BeFalse();
        }
        
        [Test]
        public void Validator_WhenSecondHlaStringNull_ShouldHaveValidationError()
        {
            var searchHla = new LocusSearchHla
            {
                SearchHla1 = "not-null",
                SearchHla2 = null
            };
            var result = validator.Validate(searchHla);
            result.IsValid.Should().BeFalse();
        }
        
        [Test]
        public void Validator_WhenBothHlaStringsNull_ShouldHaveValidationError()
        {
            var searchHla = new LocusSearchHla
            {
                SearchHla1 = null,
                SearchHla2 = null
            };
            var result = validator.Validate(searchHla);
            result.IsValid.Should().BeFalse();
        }
    }
}