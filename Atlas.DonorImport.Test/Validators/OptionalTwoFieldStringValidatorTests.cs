using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Validators;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Validators
{
    [TestFixture]
    internal class OptionalTwoFieldStringValidatorTests
    {
        private const Locus TestLocus = Locus.C;
        private const HlaFieldType TestHlaFieldType = HlaFieldType.Dna;

        private OptionalTwoFieldStringValidator validator;

        private static readonly IEnumerable<string> EmptyStrings = new[] { null, "" };

        [SetUp]
        public void SetUp()
        {
            validator = new OptionalTwoFieldStringValidator(TestLocus, TestHlaFieldType);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("field-2")]
        public void Validate_FieldOneIsNotEmpty_ReturnsValid(string field2)
        {
            var twoFieldData = new TwoFieldStringData
            {
                Field1 = "field-1",
                Field2 = field2
            };

            var result = validator.Validate(twoFieldData);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validate_BothFieldsAreEmpty_ReturnsValid(
            [ValueSource(nameof(EmptyStrings))] string emptyField1,
            [ValueSource(nameof(EmptyStrings))] string emptyField2)
        {
            var twoFieldData = new TwoFieldStringData
            {
                Field1 = emptyField1,
                Field2 = emptyField2
            };

            var result = validator.Validate(twoFieldData);

            result.IsValid.Should().BeTrue();
        }

        [TestCaseSource(nameof(EmptyStrings))]
        public void Validate_FieldOneIsEmpty_AndFieldTwoIsNotEmpty_ReturnsInvalid(string emptyField1)
        {
            var twoFieldData = new TwoFieldStringData
            {
                Field1 = emptyField1,
                Field2 = "field-2"
            };

            var result = validator.Validate(twoFieldData);

            result.IsValid.Should().BeFalse();
        }

        [TestCaseSource(nameof(EmptyStrings))]
        public void Validate_FieldOneIsEmpty_AndFieldTwoIsNotEmpty_ReturnsErrorMessagePrefixedWithLocusNameAndHlaFieldType(string emptyField1)
        {
            var twoFieldData = new TwoFieldStringData
            {
                Field1 = emptyField1,
                Field2 = "field-2"
            };

            var result = validator.Validate(twoFieldData);

            string.Join(";", result.Errors).Should().StartWith($"Optional locus {TestLocus}, {TestHlaFieldType}");
        }
    }
}