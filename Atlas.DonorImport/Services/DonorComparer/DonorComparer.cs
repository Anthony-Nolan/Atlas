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
using Atlas.DonorImport.FileSchema.Models.DonorComparer;
using MoreLinq;

namespace Atlas.DonorImport.Services.DonorComparer
{
    public interface IDonorComparer
    {
        Task CompareDonorsFromFile(DonorImportFile file);
    }

    internal class DonorComparer : IDonorComparer
    {
        private const int BatchSize = 10000;
        private readonly IDonorImportFileParser fileParser;
        private readonly IDonorReader donorReader;
        private readonly IDonorCheckerBlobStorageClient blobStorageClient;
        private readonly IDonorCheckerMessageSender messageSender;
        private readonly INotificationSender notificationSender;
        private readonly ILogger logger;
        private readonly IDonorRecordChangeApplier donorRecordChangeApplier;

        public DonorComparer(IDonorImportFileParser fileParser, IDonorReader donorReader, IDonorCheckerBlobStorageClient blobStorageClient, IDonorCheckerMessageSender messageSender, INotificationSender notificationSender, ILogger logger, IDonorRecordChangeApplier donorRecordChangeApplier)
        {
            this.fileParser = fileParser;
            this.donorReader = donorReader;
            this.blobStorageClient = blobStorageClient;
            this.messageSender = messageSender;
            this.notificationSender = notificationSender;
            this.logger = logger;
            this.donorRecordChangeApplier = donorRecordChangeApplier;
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

                    var diffs = donors.Select(d => (d.RecordId, donorRecordChangeApplier.MapToDatabaseDonor(d, file.FileLocation).Hash))
                        .Where(d => !donorsHashes.ContainsKey(d.RecordId) || !string.Equals(d.Hash, donorsHashes[d.RecordId]));

                    donorComparerResults.DifferentDonorRecordIds.AddRange(diffs.Select(d => d.RecordId));

                    checkedDonorsCount += donors.Count;
                    logger.SendTrace($"Batch complete - compared {donors.Count} donor(s) this batch. Cumulatively {checkedDonorsCount} donor(s). ");
                }

                if (donorComparerResults.DifferentDonorRecordIds.Count > 0)
                {
                    await blobStorageClient.UploadResults(donorComparerResults, filename);
                }

                logger.SendTrace($"Donor Comparison for file '{file.FileLocation}' complete. Checked {checkedDonorsCount} donor(s). Found {donorComparerResults.DifferentDonorRecordIds.Count} mismatches.");

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
            catch (DonorFormatException e)
            {
                await LogFileErrorAndSendAlert(e.Message, e.InnerException?.Message);
            }
            catch (Exception e)
            {
                logger.SendEvent(new DonorComparerFailureEventModel(file, e));

                throw;
            }
        }

        private async Task LogFileErrorAndSendAlert(string message, string description)
        {
            logger.SendTrace(message, LogLevel.Warn);
            await notificationSender.SendAlert(message, description, Priority.Medium, nameof(CompareDonorsFromFile));
        }
    }
}
