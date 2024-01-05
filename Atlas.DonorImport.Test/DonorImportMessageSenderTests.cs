using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Notifications;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Logger;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Dasync.Collections;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using NUnit;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.ExternalInterface.Settings;

namespace Atlas.DonorImport.Test
{
    [TestFixture]
    public class DonorImportMessageSenderTests
    {
        private IDonorImportFileParser fileParser;
        private IDonorRecordChangeApplier donorRecordChangeApplier;
        private IDonorImportFileHistoryService donorImportFileHistoryService;
        private INotificationSender notificationSender;
        private IDonorImportLogService donorLogService;
        private IDonorUpdateCategoriser donorUpdateCategoriser;
        private IDonorImportLogger<DonorImportLoggingContext> logger;
        private IDonorImportMessageSender donorImportMessageSender;
        private DonorImportSettings donorImportSettings;

        private IDonorFileImporter donorFileImporter;

        [SetUp]
        public void SetUp()
        {
            fileParser = Substitute.For<IDonorImportFileParser>();
            donorRecordChangeApplier = Substitute.For<IDonorRecordChangeApplier>();
            donorImportFileHistoryService = Substitute.For<IDonorImportFileHistoryService>();
            notificationSender = Substitute.For<INotificationSender>();
            donorLogService = Substitute.For<IDonorImportLogService>();
            donorUpdateCategoriser = Substitute.For<IDonorUpdateCategoriser>();
            logger = Substitute.For<IDonorImportLogger<DonorImportLoggingContext>>();
            donorImportMessageSender = Substitute.For<IDonorImportMessageSender>();

            donorImportSettings = new DonorImportSettings();

            donorFileImporter = new DonorFileImporter(fileParser, donorRecordChangeApplier, donorImportFileHistoryService, notificationSender,
                donorLogService, donorUpdateCategoriser, new DonorImportLoggingContext(), logger, donorImportMessageSender, donorImportSettings);
        }

        [TestCase(UpdateMode.Differential, true)]
        [TestCase(UpdateMode.Differential, false)]
        [TestCase(UpdateMode.Full, true)]
        public async Task ImportDonorFile_SendsSuccessResultMessage(UpdateMode mode, bool allowFullModeImport)
        {
            const int validDonorCount = 3;
            var validDonors = DonorUpdateBuilder.New.Build(validDonorCount).ToList();
            const int invalidDonorsCount = 5;
            var invalidDonors = DonorUpdateBuilder.New.Build(invalidDonorsCount).ToList();
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonorsAndUpdateMode(UpdateMode.Differential, validDonors.Concat(invalidDonors).ToArray()).Build();
            var invalidDonorsWithErrors = invalidDonors.Select(d => new SearchableDonorValidationResult { DonorUpdate = d }).ToList();
            fileParser.PrepareToLazilyParseDonorUpdates(file.Contents).Returns(new LazilyParsingDonorFile(file.Contents));
            donorUpdateCategoriser.Categorise(Arg.Any<IEnumerable<DonorUpdate>>(), Arg.Any<string>())
                .Returns(new DonorUpdateCategoriserResults
                {
                    InvalidDonors = invalidDonorsWithErrors,
                    ValidDonors = validDonors
                });
            donorLogService.FilterDonorUpdatesBasedOnUpdateTime(Arg.Any<IEnumerable<DonorUpdate>>(), Arg.Any<DateTime>())
                .Returns(validDonors.ToAsyncEnumerable());
            donorImportSettings.AllowFullModeImport = allowFullModeImport;

            await donorFileImporter.ImportDonorFile(file);

            await donorImportMessageSender.Received().SendSuccessMessage(file.FileLocation, validDonorCount, Arg.Is<List<SearchableDonorValidationResult>>(r => r.Count == invalidDonorsCount));
        }

        [Test]
        public async Task ImportDonorFile_WhenUpdateModeIsFullAndItsNotAllowed_SendsFailedResultMessageAndAlert()
        {
            var donorsCount = 3;
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonorsAndUpdateMode(UpdateMode.Full, DonorUpdateBuilder.New.Build(donorsCount).ToArray()).Build(); 
            fileParser.PrepareToLazilyParseDonorUpdates(file.Contents).Returns(new LazilyParsingDonorFile(file.Contents));
            donorImportSettings.AllowFullModeImport = false;

            await donorFileImporter.ImportDonorFile(file);
            await donorImportMessageSender.Received().SendFailureMessage(file.FileLocation, ImportFailureReason.ErrorDuringImport,
                Arg.Any<string>());

            await notificationSender.ReceivedWithAnyArgs().SendAlert(default, default, Priority.Medium, default);
            await donorImportFileHistoryService.ReceivedWithAnyArgs().RegisterFailedDonorImportWithPermanentError(default);
        }

        [Test]
        public async Task ImportDonorFile_WhenEmptyDonorFileException_SendsFailedResultMessageAndAlert()
        {
            var file = DonorImportFileBuilder.NewWithDefaultContents.With(x => x.FileLocation, "name-of-the-file.ext").Build();
            var lazeFile = Substitute.For<ILazilyParsingDonorFile>();
            lazeFile.ReadLazyDonorUpdates().Throws(new EmptyDonorFileException());
            fileParser.PrepareToLazilyParseDonorUpdates(Arg.Any<Stream>()).Returns(lazeFile);


            await donorFileImporter.ImportDonorFile(file);

            await donorImportMessageSender.Received().SendFailureMessage(file.FileLocation, ImportFailureReason.ErrorDuringImport,
                "Donor file was present but it was empty.");

            await notificationSender.Received().SendAlert("Donor file was present but it was empty.", $"Donor file: name-of-the-file.ext", Priority.Medium, Arg.Any<string>());
        }

        [Test]
        public async Task ImportDonorFile_WhenMalformedDonorFileException_SendsFailedResultMessage()
        {
            var file = DonorImportFileBuilder.NewWithDefaultContents.Build();
            var lazeFile = Substitute.For<ILazilyParsingDonorFile>();
            const string error = "Malformed Donor File Exception";
            lazeFile.ReadLazyDonorUpdates().Throws(new MalformedDonorFileException(error));
            fileParser.PrepareToLazilyParseDonorUpdates(Arg.Any<Stream>()).Returns(lazeFile);


            await donorFileImporter.ImportDonorFile(file);

            await donorImportMessageSender.Received().SendFailureMessage(file.FileLocation, ImportFailureReason.ErrorDuringImport, error);
        }

        [Test]
        public async Task ImportDonorFile_WhenDonorFormatException_SendsFailedResultMessage()
        {
            var file = DonorImportFileBuilder.NewWithDefaultContents.Build();
            var lazeFile = Substitute.For<ILazilyParsingDonorFile>();
            lazeFile.ReadLazyDonorUpdates().Throws(new DonorFormatException(new Exception()));
            fileParser.PrepareToLazilyParseDonorUpdates(Arg.Any<Stream>()).Returns(lazeFile);


            await donorFileImporter.ImportDonorFile(file);

            await donorImportMessageSender.Received().SendFailureMessage(file.FileLocation, ImportFailureReason.ErrorDuringImport, "Error parsing Donor Format");
        }

        [Test]
        public async Task ImportDonorFile_WhenDuplicateDonorException_SendsFailedResultMessage()
        {
            var file = DonorImportFileBuilder.NewWithDefaultContents.Build();
            var lazeFile = Substitute.For<ILazilyParsingDonorFile>();
            const string error = "Duplicate Donor Exception";
            lazeFile.ReadLazyDonorUpdates().Throws(new DuplicateDonorException(error));
            fileParser.PrepareToLazilyParseDonorUpdates(Arg.Any<Stream>()).Returns(lazeFile);


            await donorFileImporter.ImportDonorFile(file);

            await donorImportMessageSender.Received().SendFailureMessage(file.FileLocation, ImportFailureReason.ErrorDuringImport, error);
        }

        [Test]
        public async Task ImportDonorFile_WhenDonorNotFoundException_SendsFailedResultMessage()
        {
            var file = DonorImportFileBuilder.NewWithDefaultContents.Build();
            var lazeFile = Substitute.For<ILazilyParsingDonorFile>();
            const string error = "Donor Not Found Exception";
            lazeFile.ReadLazyDonorUpdates().Throws(new DonorNotFoundException(error));
            fileParser.PrepareToLazilyParseDonorUpdates(Arg.Any<Stream>()).Returns(lazeFile);


            await donorFileImporter.ImportDonorFile(file);

            await donorImportMessageSender.Received().SendFailureMessage(file.FileLocation, ImportFailureReason.ErrorDuringImport, error);
        }


        [Test]
        public async Task ImportDonorFile_WhenUnexpectedException_SendsFailedResultMessage()
        {
            var file = DonorImportFileBuilder.NewWithDefaultContents.Build();
            var lazeFile = Substitute.For<ILazilyParsingDonorFile>();
            const string error = "Unexpected Exception";
            lazeFile.ReadLazyDonorUpdates().Throws(new Exception(error));
            fileParser.PrepareToLazilyParseDonorUpdates(Arg.Any<Stream>()).Returns(lazeFile);
            
            try
            {
                await donorFileImporter.ImportDonorFile(file);
            }
            catch
            {
                // ignored
            }

            await donorImportMessageSender.Received().SendFailureMessage(file.FileLocation, ImportFailureReason.ErrorDuringImport, error);
        }

        [Test]
        public async Task ImportDonorFile_DisposeIsCalledOnILazilyParsingDonorFile()
        {
            var file = DonorImportFileBuilder.NewWithDefaultContents.Build();
            var lazeFile = Substitute.For<ILazilyParsingDonorFile>();
            lazeFile.ReadLazyDonorUpdates().Throws(new Exception());
            fileParser.PrepareToLazilyParseDonorUpdates(Arg.Any<Stream>()).Returns(lazeFile);

            try
            {
                await donorFileImporter.ImportDonorFile(file);
            }
            catch
            {
                // ignored
            }

            lazeFile.Received().Dispose();
        }

    }
}
