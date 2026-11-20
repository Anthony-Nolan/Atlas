using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.DonorImport.Validators;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Validators
{
    [TestFixture]
    internal class SearchableDonorValidatorTests
    {
        private static readonly IEnumerable<ImportDonorChangeType> NonDeleteChangeTypes = Enum.GetValues<ImportDonorChangeType>()
            .Except(new[] { ImportDonorChangeType.Delete });

        private static readonly IEnumerable<ImportedHla> EmptyHla = new[]
        {
            null,
            new ImportedHla()
        };

        private Fixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
        }

        [TestCaseSource(nameof(EmptyHla))]
        public void Validate_ChangeTypeIsDelete_AndHlaIsEmpty_ReturnsValid(ImportedHla emptyHla)
        {
            var donorUpdate = DonorUpdateBuilder.NoHla
                .With(x => x.ChangeType, ImportDonorChangeType.Delete)
                .WithHla(emptyHla)
                .Build();

            var result = CreateValidatorWithContext(donorUpdate.RecordId).Validate(donorUpdate);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validate_ChangeTypeIsDelete_AndHlaIsNotEmpty_ReturnsValid()
        {
            var donorUpdate = DonorUpdateBuilder.New
                .With(x => x.ChangeType, ImportDonorChangeType.Delete)
                .Build();

            var result = CreateValidatorWithContext(donorUpdate.RecordId).Validate(donorUpdate);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validate_ChangeTypeIsNotDelete_AndHlaIsEmpty_ReturnsInvalid(
            [ValueSource(nameof(NonDeleteChangeTypes))] ImportDonorChangeType changeType,
            [ValueSource(nameof(EmptyHla))] ImportedHla emptyHla)
        {
            var donorUpdate = DonorUpdateBuilder.NoHla
                .With(x => x.ChangeType, changeType)
                .WithHla(emptyHla)
                .Build();

            var result = CreateValidatorWithContext(donorUpdate.RecordId).Validate(donorUpdate);

            result.IsValid.Should().BeFalse();
        }

        [TestCaseSource(nameof(NonDeleteChangeTypes))]
        public void Validate_ChangeTypeIsNotDelete_AndHlaIsNotEmpty_ReturnsValid(ImportDonorChangeType changeType)
        {
            var donorUpdate = DonorUpdateBuilder.New
                .With(x => x.ChangeType, changeType)
                .Build();

            var result = CreateValidatorWithContext(changeType != ImportDonorChangeType.Create ? donorUpdate.RecordId : string.Empty).Validate(donorUpdate);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validate_ChangeTypeIsEdit_AndDonorRecordIdIsInContext_ReturnsValid()
        {
            var donorUpdate = DonorUpdateBuilder.New
                .With(x => x.ChangeType, ImportDonorChangeType.Edit)
                .Build();

            var result = CreateValidatorWithContext(donorUpdate.RecordId).Validate(donorUpdate);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void Validate_ChangeTypeIsEdit_AndDonorRecordIdIsNotInContext_ReturnsInvalid()
        {
            var donorUpdate = DonorUpdateBuilder.New
                .With(x => x.ChangeType, ImportDonorChangeType.Edit)
                .Build();

            var result = CreateValidatorWithContext(string.Empty).Validate(donorUpdate);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void Validate_ChangeTypeIsCreate_AndDonorRecordIdIsInContext_ReturnsInvalid()
        {
            var donorUpdate = DonorUpdateBuilder.New
                .With(x => x.ChangeType, ImportDonorChangeType.Create)
                .Build();

            var result = CreateValidatorWithContext(donorUpdate.RecordId).Validate(donorUpdate);

            result.IsValid.Should().BeFalse();
        }


        [Test]
        public void Validate_ChangeTypeIsCreate_AndUpdateModeIsFull_AndDonorRecordIdIsInContext_ReturnsValid()
        {
            var donorUpdate = DonorUpdateBuilder.New
                .With(x => x.ChangeType, ImportDonorChangeType.Create)
                .With(x => x.UpdateMode, UpdateMode.Full)
                .Build();

            var result = CreateValidatorWithContext(donorUpdate.RecordId).Validate(donorUpdate);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void IsCheckedAgainstDonorStore_IsTrueExactlyWhenValidationResultDependsOnDonorStore(
            [Values] ImportDonorChangeType changeType,
            [Values] UpdateMode updateMode)
        {
            var donorUpdate = DonorUpdateBuilder.New
                .With(x => x.ChangeType, changeType)
                .With(x => x.UpdateMode, updateMode)
                .Build();
            var isValidWhenDonorIsStored = CreateValidatorWithContext(donorUpdate.RecordId).Validate(donorUpdate).IsValid;
            var isValidWhenDonorIsNotStored = CreateValidatorWithContext(fixture.Create<string>()).Validate(donorUpdate).IsValid;

            var isChecked = SearchableDonorValidator.IsCheckedAgainstDonorStore(donorUpdate);

            isChecked.Should().Be(isValidWhenDonorIsStored != isValidWhenDonorIsNotStored);
        }

        private static SearchableDonorValidator CreateValidatorWithContext(string donorRecordId) =>
            new(new SearchableDonorValidatorContext(new HashSet<string> { donorRecordId }));
    }
}