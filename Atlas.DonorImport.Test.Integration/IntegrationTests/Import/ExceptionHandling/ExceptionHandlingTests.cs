using System.Reflection;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic.FileIO;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import.ExceptionHandling
{
    [TestFixture]
    public class ExceptionHandlingTests
    {
        private IDonorFileImporter donorFileImporter;
        private ILogger mockLogger;
        private INotificationSender mockNotificationSender;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
                mockLogger = DependencyInjection.DependencyInjection.Provider.GetService<ILogger>();
                mockNotificationSender = DependencyInjection.DependencyInjection.Provider.GetService<INotificationSender>();
            });
        }

        [Test]
        public async Task ImportDonors_WithEmptyFile_SwallowsErrorAndCompletesSuccessfully()
        {
            var donorFile = DonorImportFileBuilder.NewWithoutContents.Build();

            await donorFileImporter.ImportDonorFile(donorFile);
            
            await mockNotificationSender.Received().SendAlert("Donor file was present but it was empty.", Arg.Any<string>(), Arg.Any<Priority>(), Arg.Any<string>());
        }

        [Test]
        public async Task ImportDonors_WithUnexpectedColumns_SwallowsErrorAndCompletesSuccessfully()
        {
            var malformedDonorFile = DonorImportFileWithMissingFieldBuilder.New.Build();
            var file = new DonorImportFile {Contents = malformedDonorFile.ToStream()};

            await donorFileImporter.ImportDonorFile(file);

            await mockNotificationSender.Received().SendAlert("Error parsing Donor Format", Arg.Any<string>(), Arg.Any<Priority>(), Arg.Any<string>());
        }

        [Test]
        public async Task ImportDonors_WithoutUpdateColumn_SwallowsErrorAndCompletesSuccessfully()
        {
            var malformedFile = DonorImportFileWithNoUpdateBuilder.New.Build();
            var file = new DonorImportFile {Contents = malformedFile.ToStream()};

            await donorFileImporter.ImportDonorFile(file);

            await mockNotificationSender.Received().SendAlert("Update Mode must be provided before donor list in donor import JSON file.", Arg.Any<string>(), Arg.Any<Priority>(), Arg.Any<string>());
        }

        [Test]
        public async Task ImportDonors_WithoutDonorsColumn_CompletesSuccessfully()
        {
            var malformedFile = DonorImportFileWithNoDonorsBuilder.New.Build();
            var file = new DonorImportFile {Contents = malformedFile.ToStream()};

            await donorFileImporter.ImportDonorFile(file);
            
            mockLogger.Received().SendTrace("Donor Import for file '' complete. Imported 0 donor(s).");
        }
        
        [Test]
        public async Task ImportDonors_WithoutADonorColumn_SwallowsErrorAndCompletesSuccessfully()
        {
            const int numberOfDonors = 5;
            var malformedFile = DonorImportFileWithMissingFieldBuilder.New.WithDonorCount(numberOfDonors).Build();
            var file = new DonorImportFile {Contents = malformedFile.ToStream(), FileLocation = "file-location"};

            await donorFileImporter.ImportDonorFile(file);
            await mockNotificationSender.Received().SendAlert("Donor property RecordId cannot be null.", Arg.Any<string>(), Arg.Any<Priority>(), Arg.Any<string>());
        }

        [Test]
        public async Task ImportDonors_WithInvalidEnumAtUpdateInFile_SwallowsErrorAndCompletesSuccessfully()
        {
            var malformedFile = DonorImportFileWithInvalidEnumBuilder.New.Build();
            var file = new DonorImportFile {Contents = malformedFile.ToStream()};

            await donorFileImporter.ImportDonorFile(file);
            await mockNotificationSender.Received().SendAlert("Error parsing Donor Format", Arg.Any<string>(), Arg.Any<Priority>(), Arg.Any<string>());
        }

        [Test]
        public async Task ImportDonors_WithInvalidEnumAtDonorUpdate_SwallowsErrorAndCompletesSuccessfully()
        {
            var malformedFile = DonorImportFileWithInvalidEnumBuilder.New.WithInvalidEnumDonor().Build();
            var file = new DonorImportFile {Contents = malformedFile.ToStream()};

            await donorFileImporter.ImportDonorFile(file);
            await mockNotificationSender.Received().SendAlert("Error parsing Donor Format", Arg.Any<string>(), Arg.Any<Priority>(), Arg.Any<string>());
        }

        [Test]
        public async Task ImportDonors_WithInvalidJsonFile_SwallowsErrorAndCompletesSuccessfully()
        {
            var donorTestFilePath = $"{typeof(ExceptionHandlingTests).Namespace}.MalformedImport.json";
            await using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(donorTestFilePath))
            {
                await donorFileImporter.ImportDonorFile(new DonorImportFile {Contents = stream, FileLocation = donorTestFilePath});
            }
            await mockNotificationSender.Received().SendAlert("Invalid JSON was encountered", Arg.Any<string>(), Arg.Any<Priority>(), Arg.Any<string>());
        }
    }
}