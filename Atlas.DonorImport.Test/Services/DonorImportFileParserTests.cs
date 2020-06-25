using System;
using System.Linq;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using EnumStringValues;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services
{
    [TestFixture]
    internal class DonorImportFileParserTests
    {
        private IDonorImportFileParser donorImportFileParser;

        [SetUp]
        public void SetUp()
        {
            donorImportFileParser = new DonorImportFileParser();
        }

        [Test]
        public void ImportDonorFile_ProcessesAllDonors()
        {
            const int donorCount = 100;
            var fileStream = DonorImportFileContentsBuilder.New.WithDonorCount(donorCount).Build().ToStream();

            var donors = donorImportFileParser.PrepareToLazilyParseDonorUpdates(fileStream).ReadLazyDonorUpdates().ToList();

            donors.Should().HaveCount(donorCount);
        }

        [Test]
        public void ImportDonorFile_FullModeWithAdditionsOnly_DoesNotThrow()
        {
            var donors =
                DonorUpdateBuilder.New
                    .With(donor => donor.ChangeType, ImportDonorChangeType.Create)
                    .Build(10);
            var fileStream = DonorImportFileContentsBuilder.New
                .With(file => file.updateMode, UpdateMode.Full)
                .WithDonors(donors.ToArray())
                .Build().ToStream();

            var lazyParsedDonors = donorImportFileParser.PrepareToLazilyParseDonorUpdates(fileStream).ReadLazyDonorUpdates();

            lazyParsedDonors.Invoking(lazyDonors => lazyDonors.ToList()).Should().NotThrow();
        }

        [Test]
        public void ImportDonorFile_FullModeWithNonAdditions_Throws()
        {
            var variedDonors =
                DonorUpdateBuilder.New
                    .With(donor => donor.ChangeType, EnumExtensions.EnumerateValues<ImportDonorChangeType>())
                    .Build(10);
            var fileStream = DonorImportFileContentsBuilder.New
                .With(file => file.updateMode, UpdateMode.Full)
                .WithDonors(variedDonors.ToArray())
                .Build().ToStream();

            var lazyParsedDonors = donorImportFileParser.PrepareToLazilyParseDonorUpdates(fileStream).ReadLazyDonorUpdates();
            lazyParsedDonors.Invoking(lazyDonors => lazyDonors.ToList()).Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void ImportDonorFile_DiffModeWithNonAdditions_DoesNotThrow()
        {
            var variedDonors =
                DonorUpdateBuilder.New
                    .With(donor => donor.ChangeType, EnumExtensions.EnumerateValues<ImportDonorChangeType>())
                    .Build(10);
            var fileStream = DonorImportFileContentsBuilder.New
                .With(file => file.updateMode, UpdateMode.Differential)
                .WithDonors(variedDonors.ToArray())
                .Build().ToStream();
            var lazyParsedDonors = donorImportFileParser.PrepareToLazilyParseDonorUpdates(fileStream).ReadLazyDonorUpdates();

            lazyParsedDonors.Invoking(lazyDonors => lazyDonors.ToList()).Should().NotThrow();
        }
    }
}