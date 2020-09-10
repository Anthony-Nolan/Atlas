using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import
{
    /// <summary>
    /// These tests exist for when a donor import has stalled and is being retried.
    /// </summary>
    [TestFixture]
    public class StalledDonorImportTests
    {
        private IDonorFileImporter donorFileImporter;
        private IDonorImportFileHistoryService donorImportFileHistoryService;
        private IDonorInspectionRepository donorRepository;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                DatabaseManager.ClearDatabases();
                donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
                donorImportFileHistoryService = DependencyInjection.DependencyInjection.Provider.GetService<IDonorImportFileHistoryService>();
                donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                // Ensure any mocks set up for this test do not stick around.
                DependencyInjection.DependencyInjection.BackingProvider = DependencyInjection.ServiceConfiguration.CreateProvider();
                DatabaseManager.ClearDatabases();
            });
        }

        [Test]
        public async Task ImportDonors_WhenRetryingImportOfFile_DoesNotThrow()
        {
            const string fileName = "Donor-File";
            const string messageId = "Message-Id";
            var uploadTime = DateTime.Now;

            // Set up initial stalled import
            var donorFile = DonorImportFileBuilder.NewWithMetadata(fileName, messageId, uploadTime).WithDonorCount(0).Build();
            await donorFileImporter.ImportDonorFile(donorFile);
            await donorImportFileHistoryService.RegisterStartOfDonorImport(donorFile);

            var donorFileRetry = DonorImportFileBuilder.NewWithMetadata(fileName, messageId, uploadTime).WithDonorCount(10).Build();
            await donorFileImporter.ImportDonorFile(donorFileRetry);

            donorRepository.StreamAllDonors().Count().Should().Be(10);
        }

        [Test]
        public async Task ImportDonors_WhenRetryingImportOfFileWithDifferentMessageId_ShouldNotUploadDonors()
        {
            const string fileName = "Donor-File";
            const string messageId = "Message-Id";
            var uploadTime = DateTime.Now;

            // Set up initial stalled import
            var donorFile = DonorImportFileBuilder.NewWithMetadata(fileName, messageId, uploadTime).WithDonorCount(0).Build();
            await donorFileImporter.ImportDonorFile(donorFile);
            await donorImportFileHistoryService.RegisterStartOfDonorImport(donorFile);

            var donorFileRetry = DonorImportFileBuilder.NewWithMetadata(fileName, "Different-Message-Id", uploadTime).WithDonorCount(10).Build();

            await donorFileImporter.Invoking(i => i.ImportDonorFile(donorFileRetry)).Should().ThrowAsync<DuplicateDonorFileImportException>();
        }
    }
}