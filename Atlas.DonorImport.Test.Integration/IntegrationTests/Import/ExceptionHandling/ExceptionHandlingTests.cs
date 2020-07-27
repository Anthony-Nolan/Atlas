using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import.ExceptionHandling
{
    [TestFixture]
    public class ExceptionHandlingTests
    {
        private IDonorInspectionRepository donorRepository;
        private IMessagingServiceBusClient serviceBusClient;
        private IDonorFileImporter donorFileImporter;
        private ILogger mockLogger;
        private INotificationSender mockNotificationSender;
        private readonly Builder<DonorImportFile> fileBuilder = DonorImportFileBuilder.NewWithoutContents;

        private readonly Builder<DonorUpdate> donorCreationBuilder =
            DonorUpdateBuilder.New
                .WithRecordIdPrefix("external-donor-code-")
                .With(upd => upd.ChangeType, ImportDonorChangeType.Create);

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
                serviceBusClient = DependencyInjection.DependencyInjection.Provider.GetService<IMessagingServiceBusClient>();
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
            var malformedDonorFile = DonorImportFileWithUnexpectedFieldBuilder.New.Build();
            var file = new DonorImportFile {Contents = malformedDonorFile.ToStream()};

            await donorFileImporter.ImportDonorFile(file);

            await mockNotificationSender.Received().SendAlert("Unrecognised property: unexpectedField encountered in donor import file.", Arg.Any<string>(), Arg.Any<Priority>(), Arg.Any<string>());
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
        public async Task ImportDonors_WithoutDonorsColumn_SwallowsErrorAndCompletesSuccessfully()
        {
            var malformedFile = DonorImportFileWithNoDonorsBuilder.New.Build();
            var file = new DonorImportFile {Contents = malformedFile.ToStream()};

            await donorFileImporter.ImportDonorFile(file);
            
            mockLogger.Received().SendTrace("Donor Import for file '' complete. Imported 0 donor(s).");
        }
    }
}