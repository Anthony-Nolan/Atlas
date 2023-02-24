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
        Task CheckDonorIdsFromFile(BlobImportFile file);
    }

    public class DonorIdChecker : IDonorIdChecker
    {
        private const int BatchSize = 2;
        private readonly IDonorIdCheckerFileParser fileParser;
        private readonly IDonorReader donorReader;
        private readonly IDonorIdCheckerBlobStorageClient blobStorageClient;
        private readonly IDonorIdCheckerMessageSender messageSender;
        private readonly INotificationSender notificationSender;
        private readonly ILogger logger;

        public DonorIdChecker(IDonorIdCheckerFileParser fileParser, IDonorReader donorReader, IDonorIdCheckerBlobStorageClient blobStorageClient, IDonorIdCheckerMessageSender messageSender, INotificationSender notificationSender, ILogger logger)
        {
            this.fileParser = fileParser;
            this.donorReader = donorReader;
            this.blobStorageClient = blobStorageClient;
            this.messageSender = messageSender;
            this.notificationSender = notificationSender;
            this.logger = logger;
        }

        public async Task CheckDonorIdsFromFile(BlobImportFile file)
        {
            var lazyFile = fileParser.PrepareToLazilyParsingDonorIdFile(file.Contents);
            var donorIdCheckResults = new DonorIdCheckerResults();
            var filename = GetResultFilename(file.FileLocation);

            try
            {
                foreach (var donorIdsBatch in lazyFile.ReadLazyDonorIds().Batch(BatchSize))
                {
                    var donorIdsList = donorIdsBatch.ToList();
                    var externalDonorCodes = await donorReader.GetExistingExternalDonorCodes(donorIdsList);
                    donorIdCheckResults.MissingDonorIds.AddRange(donorIdsList.Except(externalDonorCodes));
                }

                await blobStorageClient.UploadResults(donorIdCheckResults, filename);

                await messageSender.SendSuccessCheckMessage(file.FileLocation, filename);
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
                var donorIdCheckFailureEventModel = new DonorIdCheckFailureEventModel(file, e);

                logger.SendEvent(donorIdCheckFailureEventModel);

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
