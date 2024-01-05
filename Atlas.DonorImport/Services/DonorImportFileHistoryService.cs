using System;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.ExternalInterface.Settings;

namespace Atlas.DonorImport.Services
{
    public interface IDonorImportFileHistoryService
    {
        public Task<DonorImportHistoryRecord> RegisterStartOfDonorImport(DonorImportFile donorFile);
        public Task RegisterSuccessfulDonorImport(DonorImportFile donorFile);
        public Task RegisterSuccessfulBatchImport(DonorImportFile donorFile, int importedCount, int failedCount);
        public Task RegisterFailedDonorImportWithPermanentError(DonorImportFile donorFile);
        public Task RegisterUnexpectedDonorImportError(DonorImportFile donorFile);
        public Task SendNotificationForStalledImports();
    }
    
    public class DonorImportFileHistoryService : IDonorImportFileHistoryService
    {
        private readonly IDonorImportHistoryRepository repository;
        private readonly INotificationSender notificationSender;
        private readonly TimeSpan durationToCheckForStalledFiles;
        private readonly ILogger logger;

        public DonorImportFileHistoryService(
            IDonorImportHistoryRepository repository,
            INotificationSender notificationSender,
            DonorImportSettings stalledFileSettings,
            ILogger logger)
        {
            this.repository = repository;
            this.notificationSender = notificationSender;
            this.durationToCheckForStalledFiles = new TimeSpan(stalledFileSettings.HoursToCheckStalledFiles, 0, 0);
            this.logger = logger;
        }

        public async Task<DonorImportHistoryRecord> RegisterStartOfDonorImport(DonorImportFile donorFile)
        {
            var filename = GetFileNameFromLocation(donorFile.FileLocation);
            var importRecord = await repository.GetFileIfExists(filename, donorFile.UploadTime);

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (importRecord?.FileState)
            {
                case null:
                    await repository.InsertNewDonorImportRecord(filename, donorFile.MessageId, donorFile.UploadTime);
                    break;
                case DonorImportState.FailedUnexpectedly:
                    await UpdateDonorImportRecord(donorFile, DonorImportState.Started);
                    break;
                case DonorImportState.Stalled:
                case DonorImportState.Started:
                    if (importRecord.ServiceBusMessageId == donorFile.MessageId)
                    {
                        logger.SendTrace($"Retrying stalled Donor Import for file '{filename}'.");
                        break;
                    }
                    throw new DuplicateDonorFileImportException(filename, importRecord.FileState.ToString());
                default:
                    throw new DuplicateDonorFileImportException(filename, importRecord.FileState.ToString());
            }

            return importRecord;
        }

        public async Task RegisterSuccessfulDonorImport(DonorImportFile donorFile)
        {
            await UpdateDonorImportRecord(donorFile, DonorImportState.Completed);
        }

        public async Task RegisterSuccessfulBatchImport(DonorImportFile donorFile, int importedCount, int failedCount)
        {
            var filename = GetFileNameFromLocation(donorFile.FileLocation);
            await repository.IncrementImportedDonorCount(filename, donorFile.UploadTime, importedCount, failedCount);
        }

        public async Task RegisterFailedDonorImportWithPermanentError(DonorImportFile donorFile)
        {
            await UpdateDonorImportRecord(donorFile, DonorImportState.FailedPermanent);
        }

        public async Task SendNotificationForStalledImports()
        {
            var results = await repository.GetLongRunningFiles(durationToCheckForStalledFiles);
            foreach (var record in results)
            {
                await repository.UpdateDonorImportState(record.Filename, record.UploadTime, DonorImportState.Stalled);
                await notificationSender.SendAlert(
                    "Long running Donor Import File Found",
                    $"The file with name {record.Filename} is recorded as Started and was uploaded at {record.UploadTime}. Manual investigation is recommended. This is likely caused by either a system interruption, or a function timeout.",
                    Priority.Medium,
                    nameof(DonorImportFileHistoryService));
            }
        }

        public async Task RegisterUnexpectedDonorImportError(DonorImportFile donorFile)
        {
            await UpdateDonorImportRecord(donorFile, DonorImportState.FailedUnexpectedly);
        }

        private async Task UpdateDonorImportRecord(DonorImportFile donorFile, DonorImportState state)
        {
            var filename = GetFileNameFromLocation(donorFile.FileLocation);
            await repository.UpdateDonorImportState(filename, donorFile.UploadTime, state);
        }
        
        private static string GetFileNameFromLocation(string location)
        {
            var i = location.LastIndexOf('/');
            return location.Substring(i + 1);
        }
    }
}