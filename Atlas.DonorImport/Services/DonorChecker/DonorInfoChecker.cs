using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.DonorImport.ApplicationInsights;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.FileSchema.Models.DonorChecker;
using MoreLinq;
using Donor = Atlas.DonorImport.Data.Models.Donor;

namespace Atlas.DonorImport.Services.DonorChecker
{
    public interface IDonorInfoChecker
    {
        Task CompareDonorInfoInFileToAtlasDonorStore(DonorImportFile file);
    }

    internal class DonorInfoChecker : IDonorInfoChecker
    {
        private const int BatchSize = 10000;
        private readonly IDonorImportFileParser fileParser;
        private readonly IDonorReadRepository donorReadRepository;
        private readonly IDonorInfoCheckerBlobStorageClient blobStorageClient;
        private readonly IDonorInfoCheckerMessageSender messageSender;
        private readonly INotificationSender notificationSender;
        private readonly ILogger logger;
        private readonly IDonorUpdateMapper donorUpdateMapper;

        public DonorInfoChecker(IDonorImportFileParser fileParser, IDonorReadRepository donorReadRepository, IDonorInfoCheckerBlobStorageClient blobStorageClient, IDonorInfoCheckerMessageSender messageSender, INotificationSender notificationSender, ILogger logger, IDonorUpdateMapper donorUpdateMapper)
        {
            this.fileParser = fileParser;
            this.donorReadRepository = donorReadRepository;
            this.blobStorageClient = blobStorageClient;
            this.messageSender = messageSender;
            this.notificationSender = notificationSender;
            this.logger = logger;
            this.donorUpdateMapper = donorUpdateMapper;
        }

        public async Task CompareDonorInfoInFileToAtlasDonorStore(DonorImportFile file)
        {
            LogMessage($"Beginning Donor comparison for file '{file.FileLocation}'.");
            var lazyFile = fileParser.PrepareToLazilyParseDonorUpdates(file.Contents);
            var filename = $"{Path.GetFileNameWithoutExtension(file.FileLocation)}-{DateTime.Now:yyyyMMddhhmmssfff}.json";
            var checkedDonorsCount = 0;
            var checkerResults = new DonorCheckerResults();
            try
            {

                foreach (var donorsBatch in lazyFile.ReadLazyDonorUpdates().Batch(BatchSize))
                {
                    var donors = donorsBatch.ToList();
                    var donorsHashes = await donorReadRepository.GetDonorsHashes(donors.Select(d => d.RecordId));

                    checkerResults.DonorRecordIds.AddRange(donors.Select(d => donorUpdateMapper.MapToDatabaseDonor(d, file.FileLocation))
                        .Where(DonorIsAbsentOrHashIsDifferent).Select(d => d.ExternalDonorCode));

                    checkedDonorsCount += donors.Count;
                    LogMessage($"Batch complete - compared {donors.Count} donor(s) this batch. Cumulatively {checkedDonorsCount} donor(s). ");

                    bool DonorIsAbsentOrHashIsDifferent(Donor donor) =>
                        !donorsHashes.ContainsKey(donor.ExternalDonorCode) || !string.Equals(donor.Hash, donorsHashes[donor.ExternalDonorCode]);
                }

                if (checkerResults.DonorRecordIds.Count > 0)
                {
                    await blobStorageClient.UploadResults(checkerResults, filename);
                }

                LogMessage($"Donor Info Check for file '{file.FileLocation}' complete. Checked {checkedDonorsCount} donor(s). Found {checkerResults.DonorRecordIds.Count} differences.");

                await messageSender.SendSuccessDonorCheckMessage(file.FileLocation, checkerResults.DonorRecordIds.Count, filename);
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
            await notificationSender.SendAlert(message, description, Priority.Medium, nameof(CompareDonorInfoInFileToAtlasDonorStore));
        }

        private void LogMessage(string message) =>
            logger.SendTrace($"{nameof(DonorInfoChecker)}: {message}");
    }
}
