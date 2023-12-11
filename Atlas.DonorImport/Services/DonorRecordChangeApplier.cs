using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Notifications;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Config;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.ExternalInterface.Settings;
using Atlas.DonorImport.Models;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Models.Mapping;
using Atlas.DonorImport.Services.DonorUpdates;
using Donor = Atlas.DonorImport.Data.Models.Donor;
using Atlas.DonorImport.Helpers;

namespace Atlas.DonorImport.Services
{
    internal interface IDonorRecordChangeApplier
    {
        /// <param name="donorUpdates">Batch of donors to update</param>
        /// <param name="file">The donor import file being imported</param>
        /// <param name="failedDonorsCount">
        /// The number of donors that were processed in a batch, but have not been applied due to per-donor validation failure.
        /// This must be passed in here as we need to apply donor updates and logs in the same transaction.
        /// </param>
        Task ApplyDonorRecordChangeBatch(IReadOnlyCollection<DonorUpdate> donorUpdates, DonorImportFile file, int failedDonorsCount);
    }

    internal class DonorRecordChangeApplier : IDonorRecordChangeApplier
    {
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDonorReadRepository donorInspectionRepository;
        private readonly IDonorUpdatesSaver updatesSaver;
        private readonly IDonorImportLogService donorImportLogService;
        private readonly IDonorImportFileHistoryService donorImportHistoryService;
        private readonly INotificationSender notificationSender;
        private readonly NotificationConfigurationSettings notificationConfigSettings;
        private readonly IDonorUpdateMapper donorUpdateMapper;

        public DonorRecordChangeApplier(
            IDonorImportRepository donorImportRepository,
            IDonorReadRepository donorInspectionRepository,
            IDonorUpdatesSaver updatesSaver,
            IDonorImportLogService donorImportLogService,
            IDonorImportFileHistoryService donorImportHistoryService,
            INotificationSender notificationSender,
            NotificationConfigurationSettings notificationConfigSettings,
            IDonorUpdateMapper donorUpdateMapper)
        {
            this.donorImportRepository = donorImportRepository;
            this.donorInspectionRepository = donorInspectionRepository;
            this.updatesSaver = updatesSaver;
            this.donorImportLogService = donorImportLogService;
            this.donorImportHistoryService = donorImportHistoryService;
            this.notificationSender = notificationSender;
            this.notificationConfigSettings = notificationConfigSettings;
            this.donorUpdateMapper = donorUpdateMapper;
        }

        public async Task ApplyDonorRecordChangeBatch(IReadOnlyCollection<DonorUpdate> donorUpdates, DonorImportFile file, int failedDonorsCount)
        {
            if (!donorUpdates.Any())
            {
                await donorImportHistoryService.RegisterSuccessfulBatchImport(file, 0, failedDonorsCount);
                return;
            }

            using (var transactionScope = new AsyncTransactionScope())
            {
                var updates = await ApplyUpdatesToDonorStore(donorUpdates, file);
                await SavePublishableDonorUpdates(updates);
                await donorImportLogService.SetLastUpdated(donorUpdates, file.UploadTime);
                await donorImportHistoryService.RegisterSuccessfulBatchImport(file, donorUpdates.Count, failedDonorsCount);
                transactionScope.Complete();
            }
        }

        private async Task<List<List<SearchableDonorUpdate>>> ApplyUpdatesToDonorStore(
            IReadOnlyCollection<DonorUpdate> donorUpdates,
            DonorImportFile file)
        {
            var updateMode = DetermineUpdateMode(donorUpdates);

            var updatesByType = donorUpdates.GroupBy(du => du.ChangeType);

            var matchingComponentUpdateMessages = new List<List<SearchableDonorUpdate>>();

            foreach (var updatesOfSameOperationType in updatesByType)
            {
                var externalCodes = updatesOfSameOperationType.Select(update => update.RecordId).ToList();
                switch (updatesOfSameOperationType.Key, updateMode)
                {
                    case (ImportDonorChangeType.Create, UpdateMode.Full):
                    case (ImportDonorChangeType.Upsert, UpdateMode.Full):
                    case (ImportDonorChangeType.Upsert, _):
                        var upsertMessagesFull = await ProcessDonorUpserts(updatesOfSameOperationType.ToList(), externalCodes, file, updateMode);
                        matchingComponentUpdateMessages.Add(upsertMessagesFull);
                        break;
                    case (_, UpdateMode.Full):
                        throw new NotImplementedException(
                            "Full donor imports must only consist of donor creations. Updates and Deletions not supported (creates will be treated as upserts, deletions must be performed manually)");

                    case (ImportDonorChangeType.Create, _):
                        var creationMessages = await ProcessDonorCreations(
                            updatesOfSameOperationType.ToList(),
                            externalCodes,
                            file.FileLocation,
                            updateMode
                        );
                        matchingComponentUpdateMessages.Add(creationMessages);
                        break;

                    case (ImportDonorChangeType.Edit, _):
                        var editMessages = await ProcessDonorEdits(updatesOfSameOperationType.ToList(), externalCodes, file);
                        matchingComponentUpdateMessages.Add(editMessages);
                        break;

                    case (ImportDonorChangeType.Delete, _):
                        var deleteMessages = await ProcessDonorDeletions(externalCodes);
                        matchingComponentUpdateMessages.Add(deleteMessages);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return matchingComponentUpdateMessages;
        }

        private async Task SavePublishableDonorUpdates(IEnumerable<List<SearchableDonorUpdate>> donorUpdates)
        {
            // Batched by update mode, to make testing of combined files easier
            foreach (var donorUpdateBatch in donorUpdates.Where(donorUpdateBatch => donorUpdateBatch.Any()))
            {
                await updatesSaver.Save(donorUpdateBatch);
            }
        }

        /// <summary>
        /// Processes a collection of donor updates as UPSERTS - i.e. if the donor already exists, update it - if not, create it.
        /// </summary>
        /// <returns>A collection of donor updates to be published for import into the matching component's data store.</returns>
        private async Task<List<SearchableDonorUpdate>> ProcessDonorUpserts(
            IEnumerable<DonorUpdate> updates,
            List<string> externalCodes,
            DonorImportFile file,
            UpdateMode updateMode)
        {
            var existingDonors = await donorInspectionRepository.GetDonorIdsByExternalDonorCodes(externalCodes);
            var (edits, creates) = updates.ReifyAndSplit(du => existingDonors.ContainsKey(du.RecordId));

            var editMessages = await ProcessDonorEdits(edits, externalCodes, file);
            var createMessages = await ProcessDonorCreations(creates, externalCodes, file.FileLocation, updateMode);

            return createMessages.Concat(editMessages).ToList();
        }

        /// <returns>A collection of donor updates to be published for import into the matching component's data store.</returns>
        private async Task<List<SearchableDonorUpdate>> ProcessDonorCreations(
            List<DonorUpdate> creationUpdates,
            List<string> externalCodes,
            string fileLocation,
            UpdateMode updateMode)
        {
            var creationsWithoutAtlasIds = creationUpdates.Select(update => donorUpdateMapper.MapToDatabaseDonor(update, fileLocation)).ToList();
            await donorImportRepository.InsertDonorBatch(creationsWithoutAtlasIds);

            if (updateMode == UpdateMode.Full)
            {
                // The process of publishing and consuming update messages for the matching algorithm is very slow. 
                // For an initial load, donors will be imported in bulk to the matching algorithm, via a manually triggered process 
                return new List<SearchableDonorUpdate>();
            }

            var newAtlasDonorIds = await donorInspectionRepository.GetDonorIdsByExternalDonorCodes(externalCodes);

            return creationsWithoutAtlasIds.Select(creation =>
            {
                creation.AtlasId = GetAtlasIdFromCode(creation.ExternalDonorCode, newAtlasDonorIds);
                return SearchableDonorUpdateHelper.MapToMatchingUpdateMessage(creation);
            }).ToList();
        }

        /// <returns>A collection of donor updates to be published for import into the matching component's data store.</returns>
        private async Task<List<SearchableDonorUpdate>> ProcessDonorEdits(
            List<DonorUpdate> editUpdates,
            ICollection<string> externalCodes,
            DonorImportFile file)
        {
            var existingAtlasDonors = await donorInspectionRepository.GetDonorsByExternalDonorCodes(externalCodes);
            var existingAtlasDonorIds = existingAtlasDonors.ToDictionary(d => d.Key, d => d.Value.AtlasId);
            var existingAtlasDonorHashes = existingAtlasDonors.Select(r => r.Value.Hash).ToHashSet();

            var editedDonors = editUpdates.Select(edit =>
            {
                var dbDonor = donorUpdateMapper.MapToDatabaseDonor(edit, file.FileLocation);
                dbDonor.AtlasId = GetAtlasIdFromCode(edit.RecordId, existingAtlasDonorIds);
                return dbDonor;
            }).Where(d => !existingAtlasDonorHashes.Contains(d.Hash)).ToList();

            if (editedDonors.Count > 0)
            {
                await donorImportRepository.UpdateDonorBatch(editedDonors, file.UploadTime);
            }

            return editedDonors.Select(SearchableDonorUpdateHelper.MapToMatchingUpdateMessage).ToList();
        }

        /// <returns>A collection of donor updates to be published for import into the matching component's data store.</returns>
        private async Task<List<SearchableDonorUpdate>> ProcessDonorDeletions(List<string> deletedExternalCodes)
        {
            var deletedAtlasDonorIds = await donorInspectionRepository.GetDonorIdsByExternalDonorCodes(deletedExternalCodes);

            var nonExistentAtlasDonors = deletedExternalCodes.Except(deletedAtlasDonorIds.Keys).ToList();
            if (nonExistentAtlasDonors.Any())
            {
                if (notificationConfigSettings.NotifyOnAttemptedDeletionOfUntrackedDonor)
                {
                    await notificationSender.SendNotification(
                        "Attempted to delete donors that were not found in the Atlas database",
                        $"This does not violate the data integrity of Atlas directly, but does imply a stray between Atlas and consumer's donor collection. Donor ids: {nonExistentAtlasDonors.StringJoin(",")}.",
                        NotificationConstants.OriginatorName
                    );
                }
            }

            await donorImportRepository.DeleteDonorBatch(deletedAtlasDonorIds.Values.ToList());

            return deletedAtlasDonorIds.Values.Select(SearchableDonorUpdateHelper.MapToDeletionUpdateMessage).ToList();
        }

        private static int GetAtlasIdFromCode(string donorCode, IReadOnlyDictionary<string, int> codesToIdsDictionary)
        {
            if (!codesToIdsDictionary.TryGetValue(donorCode, out var atlasDonorId))
            {
                throw new DonorNotFoundException($"Could not find expected donor in Atlas database: {donorCode}");
            }

            return atlasDonorId;
        }

        private static UpdateMode DetermineUpdateMode(IReadOnlyCollection<DonorUpdate> donorUpdates)
        {
            if (donorUpdates.Select(u => u.UpdateMode).Distinct().Count() > 1)
            {
                // At the moment they are entirely impossible. 
                // But if someone wants to change that then they have to come and look at this code, and the things that rely on it.
                throw new InvalidOperationException("Multiple UpdateModes within a single file are not supported");
            }

            return donorUpdates.First().UpdateMode;
        }
    }
}