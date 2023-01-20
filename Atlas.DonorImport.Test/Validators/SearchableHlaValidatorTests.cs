using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Validators;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Validators
{
    [TestFixture]
    internal class SearchableHlaValidatorTests
    {
        private SearchableHlaValidator validator;

        private static readonly IEnumerable<ImportedLocus> EmptyLocus = new[]
        {
            null,
            new ImportedLocus()
        };

        [SetUp]
        public void SetUp()
        {
            validator = new SearchableHlaValidator();
        }

        [Test]
        public void Validate_AllRequiredLociAreNotEmpty_ReturnsValid()
        {
            var locus = LocusBuilder.Default.Build();

            var hla = HlaBuilder.New
                .WithImportedLocus(Locus.A, locus)
                .WithImportedLocus(Locus.B, locus)
                .WithImportedLocus(Locus.Drb1, locus)
                .Build();

            var result = validator.Validate(hla);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validate_OneRequiredLocusIsEmpty_ReturnsInvalid(
            [Values(Locus.A, Locus.B, Locus.Drb1)] Locus locus, 
            [ValueSource(nameof(EmptyLocus))] ImportedLocus emptyLocus)
        {
            var hla = HlaBuilder.Default
                .WithValidHlaAtAllLoci()
                .WithImportedLocus(locus, emptyLocus).Build();

            var result = validator.Validate(hla);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validate_OneOptionalLocusIsEmpty_ReturnsValid(
            [Values(Locus.C, Locus.Dpb1, Locus.Dqb1)] Locus locus,
            [ValueSource(nameof(EmptyLocus))] ImportedLocus emptyLocus)
        {
            var hla = HlaBuilder.Default
                .WithValidHlaAtAllLoci()
                .WithImportedLocus(locus, emptyLocus).Build();

            var result = validator.Validate(hla);

            result.IsValid.Should().BeTrue();
        }
    }
}