using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.ApplicationInsights;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Logger;
using Dasync.Collections;
using MoreLinq;

// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault

namespace Atlas.DonorImport.Services
{
    public interface IDonorFileImporter
    {
        Task ImportDonorFile(DonorImportFile file);
    }

    internal class DonorFileImporter : IDonorFileImporter
    {
        private const int BatchSize = 10000;
        private readonly IDonorImportFileParser fileParser;
        private readonly IDonorRecordChangeApplier donorRecordChangeApplier;
        private readonly IDonorImportFileHistoryService donorImportFileHistoryService;
        private readonly INotificationSender notificationSender;
        private readonly IDonorImportLogService donorLogService;
        private readonly IDonorUpdateCategoriser donorUpdateCategoriser;
        private readonly DonorImportLoggingContext loggingContext;
        private readonly ILogger logger;
        private readonly IDonorImportMessageSender donorImportMessageSender;

        public DonorFileImporter(
            IDonorImportFileParser fileParser,
            IDonorRecordChangeApplier donorRecordChangeApplier,
            IDonorImportFileHistoryService donorImportFileHistoryService,
            INotificationSender notificationSender,
            IDonorImportLogService donorLogService,
            IDonorUpdateCategoriser donorUpdateCategoriser,
            DonorImportLoggingContext loggingContext,
            IDonorImportLogger<DonorImportLoggingContext> logger,
            IDonorImportMessageSender donorImportMessageSender)
        {
            this.fileParser = fileParser;
            this.donorRecordChangeApplier = donorRecordChangeApplier;
            this.donorImportFileHistoryService = donorImportFileHistoryService;
            this.notificationSender = notificationSender;
            this.donorLogService = donorLogService;
            this.donorUpdateCategoriser = donorUpdateCategoriser;
            this.loggingContext = loggingContext;
            this.logger = logger;
            this.donorImportMessageSender = donorImportMessageSender;
        }

        public async Task ImportDonorFile(DonorImportFile file)
        {
            loggingContext.Filename = file.FileLocation;
            logger.SendTrace($"Beginning Donor Import for file '{file.FileLocation}'.");
            var importRecord = await donorImportFileHistoryService.RegisterStartOfDonorImport(file);

            var importedDonorCount = 0;
            var invalidDonorIds = new List<string>();
            var donorUpdatesToSkip = importRecord?.ImportedDonorsCount ?? 0;

            if (donorUpdatesToSkip > 0)
            {
                logger.SendTrace($"Donor Import: {donorUpdatesToSkip} donors have already been imported from this file and will be skipped.",
                    props: new Dictionary<string, string>
                    {
                        { "FileLocation", file.FileLocation }
                    });
            }

            var lazyFile = fileParser.PrepareToLazilyParseDonorUpdates(file.Contents);

            try
            {
                var donorUpdates = lazyFile.ReadLazyDonorUpdates();

                foreach (var donorUpdateBatch in donorUpdates.Skip(donorUpdatesToSkip).Batch(BatchSize))
                {
                    var categoriserResults = await donorUpdateCategoriser.Categorise(donorUpdateBatch, file.FileLocation);
                    var donorUpdatesToApply = donorLogService.FilterDonorUpdatesBasedOnUpdateTime(categoriserResults.ValidDonors, file.UploadTime);

                    var reifiedDonorBatch = await donorUpdatesToApply.ToListAsync();
                    await donorRecordChangeApplier.ApplyDonorRecordChangeBatch(reifiedDonorBatch, file, categoriserResults.InvalidDonors.Count);

                    invalidDonorIds = invalidDonorIds.Concat(categoriserResults.InvalidDonors.Select(d => d.RecordId)).ToList();
                    importedDonorCount += reifiedDonorBatch.Count;
                    logger.SendTrace($"Batch complete - imported {reifiedDonorBatch.Count} donors this batch. Cumulatively {importedDonorCount} donors. ");
                }

                await donorImportFileHistoryService.RegisterSuccessfulDonorImport(file);

                logger.SendTrace(
                    $"Donor Import for file '{file.FileLocation}' complete. Imported {importedDonorCount} donor(s). Failed to import {invalidDonorIds.Count} donor(s).",
                    LogLevel.Info,
                    invalidDonorIds.Count == 0
                        ? null
                        : new Dictionary<string, string> { { "FailedDonorIds", $"[{invalidDonorIds.StringJoin(", ")}]" } });

                await donorImportMessageSender.SendSuccessMessage(file.FileLocation, importedDonorCount, invalidDonorIds.Count);
            }
            catch (EmptyDonorFileException e)
            {
                const string summary = "Donor file was present but it was empty.";
                await SendFailedImportMessage(file.FileLocation, summary);
                await LogFileErrorAndSendAlert(file, summary, e.StackTrace);
            }
            catch (MalformedDonorFileException e)
            {
                await SendFailedImportMessage(file.FileLocation, e.Message);
                await LogFileErrorAndSendAlert(file, e.Message, e.StackTrace);
            }
            catch (DonorFormatException e)
            {
                await SendFailedImportMessage(file.FileLocation, e.Message);
                await LogFileErrorAndSendAlert(file, e.Message, e.InnerException?.Message);
            }
            catch (DuplicateDonorFileImportException e)
            {
                await SendFailedImportMessage(file.FileLocation, e.Message);
                await LogFileErrorAndSendAlert(file, e.Message, e.InnerException?.Message);
            }
            catch (DuplicateDonorException e)
            {
                await SendFailedImportMessage(file.FileLocation, e.Message);
                await LogFileErrorAndSendAlert(file, e.Message, e.InnerException?.Message);
            }
            catch (DonorNotFoundException e)
            {
                await SendFailedImportMessage(file.FileLocation, e.Message);
                await LogFileErrorAndSendAlert(file, e.Message, e.InnerException?.Message);
            }
            catch (Exception e)
            {
                await donorImportFileHistoryService.RegisterUnexpectedDonorImportError(file);
                await SendFailedImportMessage(file.FileLocation, e.Message);

                var donorImportEventModel = new DonorImportFailureEventModel(file, e, importedDonorCount, lazyFile);

                logger.SendEvent(donorImportEventModel);

                throw;
            }
        }

        private async Task LogFileErrorAndSendAlert(DonorImportFile file, string message, string description)
        {
            await donorImportFileHistoryService.RegisterFailedDonorImportWithPermanentError(file);
            logger.SendTrace(message, LogLevel.Warn);
            await notificationSender.SendAlert(message, description, Priority.Medium, nameof(ImportDonorFile));
        }

        private async Task SendFailedImportMessage(string fileName, string failureReasonDescription) =>
            await donorImportMessageSender.SendFailureMessage(fileName, ImportFaulireReason.ErrorDuringImport, failureReasonDescription);
    }
}