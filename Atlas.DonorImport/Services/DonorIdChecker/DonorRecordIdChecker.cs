using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.FileSchema.Models.DonorIdChecker;
using MoreLinq;

namespace Atlas.DonorImport.Services.DonorIdChecker
{
    public interface IDonorRecordIdChecker
    {
        Task CheckDonorIdsFromFile(BlobImportFile file);
        Task CheckDonorIdsFromFileBatch(BlobImportFile file);
    }

    public class DonorRecordIdChecker : IDonorRecordIdChecker
    {
        private const int BatchSize = 2;
        //private const int BatchSize = 25000;
        private readonly IDonorRecordIdCheckerFileParser fileParser;
        private readonly IDonorReader donorReader;
        private readonly IDonorRecordIdCheckerBlobStorageClient blobStorageClient;
        private readonly IDonorRecordIdCheckerNotificationSender notificationSender;

        public DonorRecordIdChecker(IDonorRecordIdCheckerFileParser fileParser, IDonorReader donorReader, IDonorRecordIdCheckerBlobStorageClient blobStorageClient, IDonorRecordIdCheckerNotificationSender notificationSender)
        {
            this.fileParser = fileParser;
            this.donorReader = donorReader;
            this.blobStorageClient = blobStorageClient;
            this.notificationSender = notificationSender;
        }

        public async Task CheckDonorIdsFromFile(BlobImportFile file)
        {
            var lazyFile = fileParser.PrepareToLazilyParsingDonorIdFile(file.Contents);
            var donorIdCheckResults = new DonorIdCheckerResults();
            try
            {
                foreach (var donorIdsBatch in lazyFile.ReadLazyDonorIds().Batch(BatchSize))
                {
                    var externalDonorCodes = await donorReader.GetExistingExternalDonorCodes(donorIdsBatch);
                    donorIdCheckResults.Results.AddRange(donorIdsBatch.Select(id => new DonorIdCheckerResult
                    {
                        RecordId = id,
                        IsPresentInDonorStore = externalDonorCodes.Any(c => c.Equals(id, StringComparison.InvariantCultureIgnoreCase))
                    }));
                }

                await blobStorageClient.UploadResults(donorIdCheckResults, "id-checker-results.json");

                await notificationSender.SendNotification($"Donor Record Id check was successful: {file.FileLocation}",
                    "Donors were checked for presence");
            }
            catch
            {

            }
        }

        public async Task CheckDonorIdsFromFileBatch(BlobImportFile file)
        {
            var lazyFile = fileParser.PrepareToLazilyParsingDonorIdFile(file.Contents);
            try
            {
                await blobStorageClient.InitiateUpload("batch-id-check-results.json");

                foreach (var donorIdsBatch in lazyFile.ReadLazyDonorIds().Batch(BatchSize))
                {
                    var externalDonorCodes = await donorReader.GetExistingExternalDonorCodes(donorIdsBatch);
                    var idCheckResults = donorIdsBatch.Select(id => new DonorIdCheckerResult
                    {
                        RecordId = id,
                        IsPresentInDonorStore = externalDonorCodes.Any(c => c.Equals(id, StringComparison.InvariantCultureIgnoreCase))
                    }).ToList();

                    await blobStorageClient.UploadResults(idCheckResults);
                }

                await blobStorageClient.CommitUpload();
            }
            catch
            {
                await blobStorageClient.CancelUpload();
            }
        }
    }
}
