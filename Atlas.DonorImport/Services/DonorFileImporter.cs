using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Validators;
using Dasync.Collections;

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
        private readonly SearchableDonorValidator searchableDonorValidator;

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
            searchableDonorValidator = new SearchableDonorValidator();
        }

        public async Task ImportDonorFile(DonorImportFile file)
        {
            logger.SendTrace($"Beginning Donor Import for file '{file.FileLocation}'.");
            await donorImportFileHistoryService.RegisterStartOfDonorImport(file);

            var importedDonorCount = 0;
            var lazyFile = fileParser.PrepareToLazilyParseDonorUpdates(file.Contents);

            try
            {
                var invalidDonorIds = new List<string>();
                var donorUpdates = lazyFile.ReadLazyDonorUpdates();
                var validDonors = donorUpdates.FilterAndCallbackIfFiltered(ValidateDonorIsSearchable, du => invalidDonorIds.Add(du.RecordId));
                var donorUpdatesToApply = donorLogService.FilterDonorUpdatesBasedOnUpdateTime(validDonors, file.UploadTime);
                await foreach (var donorUpdateBatch in donorUpdatesToApply.Batch(BatchSize))
                {
                    var reifiedDonorBatch = donorUpdateBatch.ToList();
                    await donorRecordChangeApplier.ApplyDonorRecordChangeBatch(reifiedDonorBatch, file);
                    importedDonorCount += reifiedDonorBatch.Count;
                }

                await donorImportFileHistoryService.RegisterSuccessfulDonorImport(file);

                logger.SendTrace(
                    $"Donor Import for file '{file.FileLocation}' complete. Imported {importedDonorCount} donor(s). Failed to import {invalidDonorIds.Count} donor(s).",
                    LogLevel.Info,
                    invalidDonorIds.Count == 0 ? null : new Dictionary<string, string> {{"FailedDonorIds", $"[{invalidDonorIds.StringJoin(", ")}]"}});

                await notificationSender.SendNotification($"Donor Import Successful: {file.FileLocation}",
                    $"Imported {importedDonorCount} donor(s). Failed to import {invalidDonorIds.Count} donor(s).",
                    nameof(ImportDonorFile)
                );
            }
            catch (EmptyDonorFileException e)
            {
                const string summary = "Donor file was present but it was empty.";
                await LogFileErrorAndSendAlert(file, summary, e.StackTrace);
            }
            catch (MalformedDonorFileException e)
            {
                await LogFileErrorAndSendAlert(file, e.Message, e.StackTrace);
            }
            catch (DonorFormatException e)
            {
                await LogFileErrorAndSendAlert(file, e.Message, e.InnerException?.Message);
            }
            catch (DuplicateDonorFileImportException e)
            {
                await LogFileErrorAndSendAlert(file, e.Message, e.InnerException?.Message);
            }
            catch (DuplicateDonorException e)
            {
                await LogFileErrorAndSendAlert(file, e.Message, e.InnerException?.Message);
            }
            catch (DonorNotFoundException e)
            {
                await LogFileErrorAndSendAlert(file, e.Message, e.InnerException?.Message);
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


        private async Task LogFileErrorAndSendAlert(DonorImportFile file, string message, string description)
        {
            await donorImportFileHistoryService.RegisterFailedDonorImportWithPermanentError(file);
            logger.SendTrace(message, LogLevel.Warn);
            await notificationSender.SendAlert(message, description, Priority.Medium, nameof(ImportDonorFile));
        }

        private bool ValidateDonorIsSearchable(DonorUpdate donorUpdate)
        {
            var validationResult = searchableDonorValidator.Validate(donorUpdate);
            logger.SendTrace($"Invalid Donor found with Id: {donorUpdate.RecordId}", LogLevel.Verbose);
            return validationResult.IsValid;
        }
    }
}