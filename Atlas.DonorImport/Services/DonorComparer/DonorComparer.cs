using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.FileSchema.Models.DonorComparer;
using Atlas.DonorImport.Models;
using MoreLinq;

namespace Atlas.DonorImport.Services.DonorComparer
{
    public interface IDonorComparer
    {
        Task CompareDonorsFromFile(DonorImportFile file);
    }

    internal class DonorComparer : IDonorComparer
    {
        private const int BatchSize = 5;
        private readonly IDonorImportFileParser fileParser;
        private readonly IDonorReader donorReader;
        private readonly IDonorCheckerBlobStorageClient blobStorageClient;
        private readonly IDonorCheckerMessageSender messageSender;
        private readonly INotificationSender notificationSender;
        private readonly ILogger logger;

        public DonorComparer(IDonorImportFileParser fileParser, IDonorReader donorReader, IDonorCheckerBlobStorageClient blobStorageClient, IDonorCheckerMessageSender messageSender, INotificationSender notificationSender, ILogger logger)
        {
            this.fileParser = fileParser;
            this.donorReader = donorReader;
            this.blobStorageClient = blobStorageClient;
            this.messageSender = messageSender;
            this.notificationSender = notificationSender;
            this.logger = logger;
        }

        public async Task CompareDonorsFromFile(DonorImportFile file)
        {
            logger.SendTrace($"Beginning Donor Id Check for file '{file.FileLocation}'.");
            var lazyFile = fileParser.PrepareToLazilyParseDonorUpdates(file.Contents);
            var filename = $"{Path.GetFileNameWithoutExtension(file.FileLocation)}-{DateTime.Now:yyyyMMddhhmmssfff}.json";
            var checkedDonorsCount = 0;
            var donorComparerResults = new DonorComparerResults();

            try
            {
                foreach (var donorsBatch in lazyFile.ReadLazyDonorUpdates().Batch(BatchSize))
                {
                    var donors = donorsBatch.ToList();
                    var donorsHashes = await donorReader.GetDonorsHashes(donors.Select(d => d.RecordId));

                    var diffs = donors.Select(d => (d.RecordId, Hash: ComputeHash(d)))
                        .Where(d => !donorsHashes.ContainsKey(d.RecordId) || !string.Equals(d.Hash, donorsHashes[d.RecordId]));

                    donorComparerResults.DifferentDonorRecordIds.AddRange(diffs.Select(d => d.RecordId));

                    checkedDonorsCount += donors.Count;
                    logger.SendTrace($"Batch complete - compared {donors.Count} donor(s) this batch. Cumulatively {checkedDonorsCount} donor(s). ");
                }

                if (donorComparerResults.DifferentDonorRecordIds.Count > 0)
                {
                    await blobStorageClient.UploadResults(donorComparerResults, filename);
                }

                logger.SendTrace($"Donor Id Check for file '{file.FileLocation}' complete. Checked {checkedDonorsCount} donor(s). Found {0} absent donor(s).");

                await messageSender.SendSuccessDonorCompareMessage(file.FileLocation, filename, donorComparerResults.DifferentDonorRecordIds.Count);
            }
            catch (EmptyDonorFileException e)
            {
                await LogFileErrorAndSendAlert("Donors file was present but it was empty.", e.StackTrace);
            }
            catch (MalformedDonorFileException e)
            {
                await LogFileErrorAndSendAlert(e.Message, e.StackTrace);
            }
            catch (Exception e)
            {
                //logger.SendEvent(new DonorIdCheckFailureEventModel(file, e));

                throw;
            }
        }

        private string ComputeHash(DonorUpdate donor) =>
            $"{donor.RecordId}|{donor.DonorType.ToDatabaseType()}|{donor.Ethnicity}|{donor.RegistryCode}|{donor.Hla.A.Dna.Field1}|{donor.Hla.A.Dna.Field2}|{donor.Hla.B.Dna.Field1}|{donor.Hla.B.Dna.Field2}|{donor.Hla.C.Dna.Field1}|{donor.Hla.C.Dna.Field2}|{donor.Hla.DPB1.Dna.Field1}|{donor.Hla.DPB1.Dna.Field2}|{donor.Hla.DQB1.Dna.Field1}|{donor.Hla.DQB1.Dna.Field2}|{donor.Hla.DRB1.Dna.Field1}|{donor.Hla.DRB1.Dna.Field2}"
                .ToMd5Hash();

        private async Task LogFileErrorAndSendAlert(string message, string description)
        {
            logger.SendTrace(message, LogLevel.Warn);
            await notificationSender.SendAlert(message, description, Priority.Medium, nameof(CompareDonorsFromFile));
        }
    }
}
