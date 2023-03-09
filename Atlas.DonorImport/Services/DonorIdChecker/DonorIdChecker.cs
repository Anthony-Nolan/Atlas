using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.DonorImport.ApplicationInsights;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.FileSchema.Models.DonorIdChecker;
using MoreLinq;

namespace Atlas.DonorImport.Services.DonorIdChecker
{
    public interface IDonorIdChecker
    {
        Task CheckDonorIdsFromFile(DonorIdCheckFile file);
    }

    internal class DonorIdChecker : IDonorIdChecker
    {
        private const int BatchSize = 10000;
        private readonly IDonorIdCheckerFileParser fileParser;
        private readonly IDonorReader donorReader;
        private readonly IDonorCheckerBlobStorageClient blobStorageClient;
        private readonly IDonorCheckerMessageSender messageSender;
        private readonly INotificationSender notificationSender;
        private readonly ILogger logger;

        public DonorIdChecker(IDonorIdCheckerFileParser fileParser, IDonorReader donorReader, IDonorCheckerBlobStorageClient blobStorageClient, IDonorCheckerMessageSender messageSender, INotificationSender notificationSender, ILogger logger)
        {
            this.fileParser = fileParser;
            this.donorReader = donorReader;
            this.blobStorageClient = blobStorageClient;
            this.messageSender = messageSender;
            this.notificationSender = notificationSender;
            this.logger = logger;
        }

        public async Task CheckDonorIdsFromFile(DonorIdCheckFile file)
        {
            logger.SendTrace($"Beginning Donor Id Check for file '{file.FileLocation}'.");
            var lazyFile = fileParser.PrepareToLazilyParsingDonorIdFile(file.Contents);
            var donorIdCheckResults = new DonorIdCheckerResults();
            var filename = GetResultFilename(file.FileLocation);
            var checkedDonorIdsCount = 0;

            try
            {
                foreach (var donorIdsBatch in lazyFile.ReadLazyDonorIds().Batch(BatchSize))
                {
                    var donorIdsList = donorIdsBatch.ToList();
                    var externalDonorCodes = await donorReader.GetExistingExternalDonorCodes(donorIdsList);
                    donorIdCheckResults.MissingRecordIds.AddRange(donorIdsList.Except(externalDonorCodes));

                    checkedDonorIdsCount += donorIdsList.Count;
                    logger.SendTrace($"Batch complete - checked {donorIdsList.Count} donor(s) this batch. Cumulatively {checkedDonorIdsCount} donor(s). ");
                }

                if (donorIdCheckResults.MissingRecordIds.Count > 0)
                {
                    await blobStorageClient.UploadResults(donorIdCheckResults, filename);
                }

                logger.SendTrace($"Donor Id Check for file '{file.FileLocation}' complete. Checked {checkedDonorIdsCount} donor(s). Found {donorIdCheckResults.MissingRecordIds.Count} absent donor(s).");

                await messageSender.SendSuccessCheckMessage(file.FileLocation, filename, donorIdCheckResults.MissingRecordIds.Count);
            }
            catch (EmptyDonorFileException e)
            {
                await LogFileErrorAndSendAlert("Donor Ids file was present but it was empty.", e.StackTrace);
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

        private string GetResultFilename(string location) =>
            $"{Path.GetFileNameWithoutExtension(location)}-{DateTime.Now:yyyyMMddhhmmssfff}.json";

        private async Task LogFileErrorAndSendAlert(string message, string description)
        {
            logger.SendTrace(message, LogLevel.Warn);
            await notificationSender.SendAlert(message, description, Priority.Medium, nameof(CheckDonorIdsFromFile));
        }
    }
}
