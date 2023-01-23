using Atlas.Common.Public.Models.GeneticData;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Validators;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Validators
{
    [TestFixture]
    internal class RequiredTwoFieldStringValidatorTests
    {
        private const Locus TestLocus = Locus.A;
        private const HlaFieldType TestHlaFieldType = HlaFieldType.Dna;

        private RequiredTwoFieldStringValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new RequiredTwoFieldStringValidator(TestLocus, TestHlaFieldType);
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
        public void Validate_FieldOneIsEmpty_ReturnsInvalid(
            [Values("", null)] string emptyField1,
            [Values("", null, "field-2")] string field2)
        {
            var twoFieldData = new TwoFieldStringData
            {
                Field1 = emptyField1,
                Field2 = field2
            };

            var result = validator.Validate(twoFieldData);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validate_FieldOneIsEmpty_ReturnsErrorMessagePrefixedWithLocusNameAndHlaFieldType(
            [Values("", null)] string emptyField1,
            [Values("", null, "field-2")] string field2)
        {
            var twoFieldData = new TwoFieldStringData
            {
                Field1 = emptyField1,
                Field2 = field2
            };

            var result = validator.Validate(twoFieldData);

            string.Join(";", result.Errors).Should().StartWith($"Required locus {TestLocus}, {TestHlaFieldType}");

        }
    }
}