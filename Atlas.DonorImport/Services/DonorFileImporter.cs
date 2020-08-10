using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.Utils;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.ExternalInterface.Models;
using MoreLinq.Extensions;

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
        private readonly ILogger logger;

        public DonorFileImporter(
            IDonorImportFileParser fileParser,
            IDonorRecordChangeApplier donorRecordChangeApplier,
            IDonorImportFileHistoryService donorImportFileHistoryService,
            INotificationSender notificationSender,
            IDonorImportLogService donorLogService,
            ILogger logger)
        {
            this.fileParser = fileParser;
            this.donorRecordChangeApplier = donorRecordChangeApplier;
            this.donorImportFileHistoryService = donorImportFileHistoryService;
            this.notificationSender = notificationSender;
            this.donorLogService = donorLogService;
            this.logger = logger;
        }

        public async Task ImportDonorFile(DonorImportFile file)
        {
            logger.SendTrace($"Beginning Donor Import for file '{file.FileLocation}'.");
            await donorImportFileHistoryService.RegisterStartOfDonorImport(file);

            var importedDonorCount = 0;
            var lazyFile = fileParser.PrepareToLazilyParseDonorUpdates(file.Contents);
            var transaction = new AsyncTransactionScope();
            
            try
            {
                var donorUpdates = lazyFile.ReadLazyDonorUpdates();
                var donorUpdatesToApply = await donorLogService.FilterDonorUpdatesBasedOnUpdateTime(donorUpdates, file.UploadTime);
                foreach (var donorUpdateBatch in donorUpdatesToApply.Batch(BatchSize))
                {
                    var reifiedDonorBatch = donorUpdateBatch.ToList();
                    await donorRecordChangeApplier.ApplyDonorRecordChangeBatch(reifiedDonorBatch, file);
                    importedDonorCount += reifiedDonorBatch.Count;
                }

                await donorImportFileHistoryService.RegisterSuccessfulDonorImport(file);
                
                transaction.Complete();
                
                logger.SendTrace($"Donor Import for file '{file.FileLocation}' complete. Imported {importedDonorCount} donor(s).");
                await notificationSender.SendNotification($"Donor Import Successful: {file.FileLocation}",
                    $"Imported {importedDonorCount} donor(s) from file {file.FileLocation}",
                    nameof(ImportDonorFile)
                );
            }
            catch (EmptyDonorFileException e)
            {
                const string summary = "Donor file was present but it was empty.";
                await LogErrorAndSendAlert(file, summary, e.StackTrace);
            }
            catch (MalformedDonorFileException e)
            {
                await LogErrorAndSendAlert(file, e.Message, e.StackTrace);
            }
            catch (DonorFormatException e)
            {
                await LogErrorAndSendAlert(file, e.Message, e.InnerException?.Message);
            }
            catch (DuplicateDonorImportException e)
            {
                logger.SendTrace(e.Message, LogLevel.Warn);
                await notificationSender.SendAlert(e.Message, e.InnerException?.Message, Priority.Medium, nameof(ImportDonorFile));
            }
            catch (Exception e)
            {
                await donorImportFileHistoryService.RegisterUnexpectedDonorImportError(file);
                var summary = $"Donor Import Failed: {file.FileLocation}";
                var description = @$"Importing donors for file: {file.FileLocation} has failed. With exception {e.Message}.
{importedDonorCount} Donors were successfully imported prior to this error and have already been stored in the Database. Any remaining donors in the file have not been stored.
The first {lazyFile?.ParsedDonorCount} Donors were able to be parsed from the file. The last Donor to be *successfully* parsed had DonorCode '{lazyFile?.LastSuccessfullyParsedDonorCode}'.
Manual investigation is recommended; see Application Insights for more information.";

                await notificationSender.SendAlert(summary, description, Priority.Medium, nameof(ImportDonorFile));

                throw;
            }
        }

        private async Task LogErrorAndSendAlert(DonorImportFile file, string message, string description)
        {
            await donorImportFileHistoryService.RegisterFailedDonorImportWithPermanentError(file);
            logger.SendTrace(message, LogLevel.Warn);
            await notificationSender.SendAlert(message, description, Priority.Medium, nameof(ImportDonorFile));
        }
    }
}