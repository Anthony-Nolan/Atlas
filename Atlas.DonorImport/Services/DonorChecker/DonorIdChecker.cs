using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Notifications;
using Atlas.DonorImport.ApplicationInsights;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.FileSchema.Models.DonorChecker;
using Atlas.DonorImport.Models;
using MoreLinq;

namespace Atlas.DonorImport.Services.DonorChecker
{
    public interface IDonorIdChecker
    {
        Task CheckDonorIdsFromFile(DonorIdCheckFile file);
    }

    internal class DonorIdChecker : IDonorIdChecker
    {
        private const int BatchSize = 10000;
        private readonly IDonorIdCheckerFileParser fileParser;
        private readonly IDonorReadRepository donorReadRepository;
        private readonly IDonorIdCheckerBlobStorageClient blobStorageClient;
        private readonly IDonorIdCheckerMessageSender messageSender;
        private readonly INotificationSender notificationSender;
        private readonly ILogger logger;

        private Dictionary<string, bool> loadedExternalDonorCodes;

        public DonorIdChecker(IDonorIdCheckerFileParser fileParser, IDonorReadRepository donorReadRepository, IDonorIdCheckerBlobStorageClient blobStorageClient, IDonorIdCheckerMessageSender messageSender, INotificationSender notificationSender, ILogger logger)
        {
            this.fileParser = fileParser;
            this.donorReadRepository = donorReadRepository;
            this.blobStorageClient = blobStorageClient;
            this.messageSender = messageSender;
            this.notificationSender = notificationSender;
            this.logger = logger;
        }

        public async Task CheckDonorIdsFromFile(DonorIdCheckFile file)
        {
            LogMessage($"Beginning Donor Id Check for file '{file.FileLocation}'.");
            var lazyFile = fileParser.PrepareToLazilyParsingDonorIdFile(file.Contents);
            var filename = GetResultFilename(file.FileLocation);
            var checkedDonorIdsCount = 0;
            var absentRecordIds = new List<string>();

            try
            {
                foreach (var recordIdsBatch in lazyFile.ReadLazyDonorIds().Batch(BatchSize))
                {
                    var externalDonorCodes = await GetOrReadExternalDonorCodes(lazyFile.DonorPool, lazyFile.DonorType);
                    var recordIdsInBatchChecked = 0;
                    foreach (var recordId in recordIdsBatch)
                    {
                        recordIdsInBatchChecked++;
                        if (externalDonorCodes.ContainsKey(recordId))
                        {
                            externalDonorCodes[recordId] = true;
                        }
                        else
                        {
                            absentRecordIds.Add(recordId);
                        }
                    }

                    checkedDonorIdsCount += recordIdsInBatchChecked;
                    LogMessage($"Batch complete - checked {recordIdsInBatchChecked} donor(s) this batch. Cumulatively {checkedDonorIdsCount} donor(s). ");
                }

                var orphanedRecordIds = (await GetOrReadExternalDonorCodes(lazyFile.DonorPool, lazyFile.DonorType)).Where(c => !c.Value)
                    .Select(c => c.Key);
                
                var donorIdCheckResults = new DonorIdCheckerResults
                {
                    RegistryCode = lazyFile.DonorPool,
                    DonorType = lazyFile.DonorType.ToString(),
                    AbsentRecordIds = absentRecordIds,
                    OrphanedRecordIds = orphanedRecordIds.ToList()
                };
                
                var resultsCount = donorIdCheckResults.OrphanedRecordIds.Count + donorIdCheckResults.AbsentRecordIds.Count;

                if (resultsCount > 0)
                {
                    using (logger.RunTimed("Donor Id Check results were uploaded."))
                    {
                        await blobStorageClient.UploadResults(donorIdCheckResults, filename);
                    }
                }

                LogMessage($"Donor Id Check for file '{file.FileLocation}' complete. Checked {checkedDonorIdsCount} donor(s). Found {donorIdCheckResults.AbsentRecordIds.Count} absent and {donorIdCheckResults.OrphanedRecordIds.Count} orphaned donor(s).");

                await messageSender.SendSuccessDonorCheckMessage(file.FileLocation, resultsCount, filename);
            }
            catch (EmptyDonorFileException)
            {
                await LogFileErrorAndSendAlert("Donor Ids file was present but it was empty.", $"Donor Ids file: {file.FileLocation}");
            }
            catch (MalformedDonorFileException e)
            {
                await LogFileErrorAndSendAlert(e.Message, e.StackTrace);
            }
            catch (Exception e)
            {
                logger.SendEvent(new DonorIdCheckFailureEventModel(file, e));

                throw;
            }
        }

        private async Task<Dictionary<string, bool>> GetOrReadExternalDonorCodes(string registryCode, ImportDonorType donorType) =>
            loadedExternalDonorCodes ??= await logger.RunTimedAsync("Donor External Codes were read.", async () => (await donorReadRepository.GetExternalDonorCodes(registryCode, donorType.ToDatabaseType())).ToDictionary(c => c, _ => false), logAtStart: true);

        private string GetResultFilename(string location) =>
            $"{Path.GetFileNameWithoutExtension(location)}-{DateTime.Now:yyyyMMddhhmmssfff}.json";

        private async Task LogFileErrorAndSendAlert(string message, string description)
        {
            logger.SendTrace(message, LogLevel.Warn);
            await notificationSender.SendAlert(message, description, Priority.Medium, nameof(CheckDonorIdsFromFile));
        }

        private void LogMessage(string message) =>
            logger.SendTrace($"{nameof(DonorIdChecker)}: {message}");

    }
}
