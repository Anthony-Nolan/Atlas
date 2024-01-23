using Atlas.Common.Notifications;
using Atlas.DonorImport.ExternalInterface.Settings;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Logger;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Atlas.DonorImport.Test.Services
{
    [TestFixture]
    public class DonorFileImporterTests
    {
        private DonorFileImporter donorFileImporter;

        private DonorImportSettings settings;

        private IDonorImportFileParser donorImportFileParser;
        private ILazilyParsingDonorFile lazilyParsingDonorFile;
        private IDonorRecordChangeApplier donorRecordChangeApplier;
        private IDonorImportFileHistoryService donorImportFileHistoryService;
        private IDonorImportMessageSender donorImportMessageSender;
        private IDonorImportLogger<DonorImportLoggingContext> logger;
        private INotificationSender notificationSender;

        [SetUp]
        public void SetUp()
        {
            settings = new DonorImportSettings();
            donorImportFileParser = Substitute.For<IDonorImportFileParser>();
            lazilyParsingDonorFile = Substitute.For<ILazilyParsingDonorFile>();
            donorRecordChangeApplier = Substitute.For<IDonorRecordChangeApplier>();
            donorImportFileHistoryService = Substitute.For<IDonorImportFileHistoryService>();
            donorImportMessageSender = Substitute.For<IDonorImportMessageSender>();
            notificationSender = Substitute.For<INotificationSender>();
            logger = Substitute.For<IDonorImportLogger<DonorImportLoggingContext>>();

            donorFileImporter = new DonorFileImporter(
                fileParser: donorImportFileParser,
                donorRecordChangeApplier: donorRecordChangeApplier,
                donorImportFileHistoryService: donorImportFileHistoryService, 
                notificationSender: notificationSender,
                null, 
                null,
                loggingContext: new DonorImportLoggingContext(), 
                logger: logger, 
                donorImportMessageSender: donorImportMessageSender, 
                settings: settings);

            donorImportFileParser.PrepareToLazilyParseDonorUpdates(default).ReturnsForAnyArgs(lazilyParsingDonorFile);
        }


        [Test]
        public async Task ImportDonorFile_WhenUpdateModeIsFull_AndAllowFullModeImportSettingIsFalse_RejectsImportFileAndSendsNotification()
        {
            var file = DonorImportFileBuilder.NewWithoutContents.Build();
            settings.AllowFullModeImport = false;
            lazilyParsingDonorFile.ReadUpdateMode().Returns(UpdateMode.Full);

            await donorFileImporter.ImportDonorFile(file);

            var expectedMessage = "Importing donors with Full mode is not allowed when allowFullModeImport is false.";
            var expectedDescription = $"Donor file: {file.FileLocation}";

            lazilyParsingDonorFile.DidNotReceiveWithAnyArgs().ReadLazyDonorUpdates();

            await donorImportFileHistoryService.Received().RegisterFailedDonorImportWithPermanentError(file);
            await notificationSender.Received().SendAlert(expectedMessage, expectedDescription, Priority.Medium, nameof(DonorFileImporter.ImportDonorFile));
            logger.Received().SendTrace(expectedMessage, Common.ApplicationInsights.LogLevel.Warn);
        }

        [TestCase(UpdateMode.Full, true)]
        [TestCase(UpdateMode.Differential, true)]
        [TestCase(UpdateMode.Differential, false)]
        public async Task ImportDonorFile_AcceptImportFile(UpdateMode updateMode, bool allowFullModelImport)
        {
            var file = DonorImportFileBuilder.NewWithoutContents.Build();
            settings.AllowFullModeImport = allowFullModelImport;
            lazilyParsingDonorFile.ReadUpdateMode().Returns(updateMode);

            await donorFileImporter.ImportDonorFile(file);

            lazilyParsingDonorFile.ReceivedWithAnyArgs().ReadLazyDonorUpdates();
            await donorImportFileHistoryService.ReceivedWithAnyArgs().RegisterSuccessfulDonorImport(default);

            await notificationSender.DidNotReceiveWithAnyArgs().SendAlert(default, default, default, default);
        }

        [Test]
        public async Task ImportDonorFile_DisposeIsCalledOnILazilyParsingDonorFile()
        {
            var file = DonorImportFileBuilder.NewWithDefaultContents.Build();
            
            await donorFileImporter.ImportDonorFile(file);

            lazilyParsingDonorFile.Received().Dispose();
        }
    }
}
