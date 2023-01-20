using System.Collections.Generic;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Validators;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Validators
{
    [TestFixture]
    internal class RequiredImportedLocusValidatorTests
    {
        private RequiredImportedLocusValidator validator;

        private static readonly IEnumerable<TwoFieldStringData> EmptyTwoFieldData = new[]
        {
            null,
            new TwoFieldStringData()
        };

        [SetUp]
        public void SetUp()
        {
            validator = new RequiredImportedLocusValidator();
        }

        [Test]
        public void Validate_DnaIsNotNull_AndSerologyIsNotNull_ReturnsValid()
        {
            var locus = LocusBuilder.Default.Build();

            var result = validator.Validate(locus);

            result.IsValid.Should().BeTrue();
        }

        [TestCaseSource(nameof(EmptyTwoFieldData))]
        public void Validate_DnaIsNotEmpty_AndSerologyIsEmpty_ReturnsValid(TwoFieldStringData emptyData)
        {
            var locus = LocusBuilder.Default
                .WithSerology(emptyData)
                .Build();

            var result = validator.Validate(locus);

            result.IsValid.Should().BeTrue();
        }

        [TestCaseSource(nameof(EmptyTwoFieldData))]
        public void Validate_DnaIsEmpty_AndSerologyIsNotEmpty_ReturnsValid(TwoFieldStringData emptyData)
        {
            var locus = LocusBuilder.Default
                .WithDna(emptyData)
                .Build();

            var result = validator.Validate(locus);

            result.IsValid.Should().BeTrue();
        }

        [TestCaseSource(nameof(EmptyTwoFieldData))]
        public void Validate_DnaIsEmpty_AndSerologyIsEmpty_ReturnsInvalid(TwoFieldStringData emptyData)
        {
            var locus = LocusBuilder.Default
                .WithDna(emptyData)
                .WithSerology(emptyData)
                .Build();

            var result = validator.Validate(locus);

            result.IsValid.Should().BeFalse();
        }
    }
}