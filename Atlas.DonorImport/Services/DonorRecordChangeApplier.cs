using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Notifications;
using Atlas.Common.Utils;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.Config;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Models.Mapping;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Donor = Atlas.DonorImport.Data.Models.Donor;

namespace Atlas.DonorImport.Services
{
    internal interface IDonorRecordChangeApplier
    {
        /// <param name="donorUpdates">Batch of donors to update</param>
        /// <param name="file">The donor import file being imported</param>
        /// <param name="skippedDonors">
        /// The number of donors that were processed in a batch, but have not been applied due to per-donor validation failure.
        /// This must be passed in here as we need to apply donor updates and logs in the same transaction - but we also need to send service bus updates, which mean that this method cannot be called from an outer transaction scope.
        /// </param>
        Task ApplyDonorRecordChangeBatch(IReadOnlyCollection<DonorUpdate> donorUpdates, DonorImportFile file, int skippedDonors);
    }

    internal class DonorRecordChangeApplier : IDonorRecordChangeApplier
    {
        private readonly IMessagingServiceBusClient messagingServiceBusClient;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDonorReadRepository donorInspectionRepository;
        private readonly IImportedLocusInterpreter locusInterpreter;
        private readonly IDonorImportLogService donorImportLogService;
        private readonly IDonorImportFileHistoryService donorImportHistoryService;
        private readonly INotificationSender notificationSender;

        public DonorRecordChangeApplier(
            IMessagingServiceBusClient messagingServiceBusClient,
            IDonorImportRepository donorImportRepository,
            IDonorReadRepository donorInspectionRepository,
            IImportedLocusInterpreter locusInterpreter,
            IDonorImportLogService donorImportLogService,
            IDonorImportFileHistoryService donorImportHistoryService,
            INotificationSender notificationSender)
        {
            this.donorImportRepository = donorImportRepository;
            this.messagingServiceBusClient = messagingServiceBusClient;
            this.donorInspectionRepository = donorInspectionRepository;
            this.locusInterpreter = locusInterpreter;
            this.donorImportLogService = donorImportLogService;
            this.donorImportHistoryService = donorImportHistoryService;
            this.notificationSender = notificationSender;
        }

        public async Task ApplyDonorRecordChangeBatch(IReadOnlyCollection<DonorUpdate> donorUpdates, DonorImportFile file, int skippedDonors)
        {
            if (!donorUpdates.Any())
            {
                await donorImportHistoryService.RegisterSuccessfulBatchImport(file, skippedDonors);
                return;
            }

            List<List<SearchableDonorUpdate>> matchingMessages;
            using (var transactionScope = new AsyncTransactionScope())
            {
                matchingMessages = await ApplyUpdatesToDonorStore(donorUpdates, file);
                await donorImportLogService.SetLastUpdated(donorUpdates, file.UploadTime);
                await donorImportHistoryService.RegisterSuccessfulBatchImport(file, donorUpdates.Count + skippedDonors);
                transactionScope.Complete();
            }

            await SendUpdatesForMatchingAlgorithm(matchingMessages);
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
                        var upsertMessages = await ProcessDonorUpserts(updatesOfSameOperationType.ToList(), externalCodes, file, updateMode);
                        matchingComponentUpdateMessages.Add(upsertMessages);
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

        private async Task SendUpdatesForMatchingAlgorithm(List<List<SearchableDonorUpdate>> donorUpdates)
        {
            // Batched by update mode, to make testing of combined files easier
            foreach (var donorUpdateBatch in donorUpdates.Where(donorUpdateBatch => donorUpdateBatch.Any()))
            {
                await messagingServiceBusClient.PublishDonorUpdateMessages(donorUpdateBatch);
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
            var creationsWithoutAtlasIds = creationUpdates.Select(update => MapToDatabaseDonor(update, fileLocation)).ToList();
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
                return MapToMatchingUpdateMessage(creation);
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
                var dbDonor = MapToDatabaseDonor(edit, file.FileLocation);
                dbDonor.AtlasId = GetAtlasIdFromCode(edit.RecordId, existingAtlasDonorIds);
                return dbDonor;
            }).Where(d => !existingAtlasDonorHashes.Contains(d.Hash)).ToList();

            if (editedDonors.Count > 0)
            {
                await donorImportRepository.UpdateDonorBatch(editedDonors, file.UploadTime);
            }

            return editedDonors.Select(MapToMatchingUpdateMessage).ToList();
        }

        /// <returns>A collection of donor updates to be published for import into the matching component's data store.</returns>
        private async Task<List<SearchableDonorUpdate>> ProcessDonorDeletions(List<string> deletedExternalCodes)
        {
            var deletedAtlasDonorIds = await donorInspectionRepository.GetDonorIdsByExternalDonorCodes(deletedExternalCodes);

            var nonExistentAtlasDonors = deletedExternalCodes.Except(deletedAtlasDonorIds.Keys).ToList();
            if (nonExistentAtlasDonors.Any())
            {
                await notificationSender.SendNotification(
                    "Attempted to delete donors that were not found in the Atlas database",
                    $"This does not violate the data integrity of Atlas directly, but does imply a stray between Atlas and consumer's donor collection. Donor ids: {nonExistentAtlasDonors.StringJoin(",")}.",
                    NotificationConstants.OriginatorName
                );
            }
            
            await donorImportRepository.DeleteDonorBatch(deletedAtlasDonorIds.Values.ToList());

            return deletedAtlasDonorIds.Values.Select(MapToDeletionUpdateMessage).ToList();
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

        private Donor MapToDatabaseDonor(DonorUpdate fileUpdate, string fileLocation)
        {
            Dictionary<string, string> CreateLogContext(Locus locus) =>
                new Dictionary<string, string>
                {
                    {"ImportFile", fileLocation},
                    {"DonorCode", fileUpdate.RecordId},
                    {"Locus", locus.ToString()}
                };

            var interpretedA = locusInterpreter.Interpret(fileUpdate.Hla.A, CreateLogContext(Locus.A));
            var interpretedB = locusInterpreter.Interpret(fileUpdate.Hla.B, CreateLogContext(Locus.B));
            var interpretedC = locusInterpreter.Interpret(fileUpdate.Hla.C, CreateLogContext(Locus.C));
            var interpretedDpb1 = locusInterpreter.Interpret(fileUpdate.Hla.DPB1, CreateLogContext(Locus.Dpb1));
            var interpretedDqb1 = locusInterpreter.Interpret(fileUpdate.Hla.DQB1, CreateLogContext(Locus.Dqb1));
            var interpretedDrb1 = locusInterpreter.Interpret(fileUpdate.Hla.DRB1, CreateLogContext(Locus.Drb1));

            var storedFileLocation = LeftTruncateTo256(fileLocation);

            var donor = new Donor
            {
                ExternalDonorCode = fileUpdate.RecordId,
                UpdateFile = storedFileLocation,
                LastUpdated = DateTimeOffset.UtcNow,
                DonorType = fileUpdate.DonorType.ToDatabaseType(),
                EthnicityCode = fileUpdate.Ethnicity,
                RegistryCode = fileUpdate.RegistryCode,
                A_1 = interpretedA.Position1,
                A_2 = interpretedA.Position2,
                B_1 = interpretedB.Position1,
                B_2 = interpretedB.Position2,
                C_1 = interpretedC.Position1,
                C_2 = interpretedC.Position2,
                DPB1_1 = interpretedDpb1.Position1,
                DPB1_2 = interpretedDpb1.Position2,
                DQB1_1 = interpretedDqb1.Position1,
                DQB1_2 = interpretedDqb1.Position2,
                DRB1_1 = interpretedDrb1.Position1,
                DRB1_2 = interpretedDrb1.Position2,
            };
            donor.Hash = donor.CalculateHash();
            return donor;
        }

        /// <summary>
        /// The UpdateFile field is a varchar(256), so we need to ensure that the string we try to store there is no more than 256 characters.
        /// If the actual file name is > 256, then there's not much we can do.
        /// But realistically if we *are* over 256, then it's more likely because the container is nested.
        /// In that case the *end* of the path is far more interesting than the start of it.
        /// So we should truncate from the left, rather than the right.
        /// </summary>
        private static string LeftTruncateTo256(string fileLocation)
        {
            if (fileLocation.Length > 256)
            {
                return new string(fileLocation.TakeLast(256).ToArray());
            }

            return fileLocation;
        }

        private static SearchableDonorUpdate MapToDeletionUpdateMessage(int deletedDonorId)
        {
            return new SearchableDonorUpdate
            {
                DonorId = deletedDonorId,
                IsAvailableForSearch = false,
                SearchableDonorInformation = null
            };
        }

        private static SearchableDonorUpdate MapToMatchingUpdateMessage(Donor updatedDonor)
        {
            return new SearchableDonorUpdate
            {
                DonorId = updatedDonor.AtlasId,
                IsAvailableForSearch = true, //Only false for deletions, which are handled separately
                SearchableDonorInformation = new SearchableDonorInformation
                {
                    DonorId = updatedDonor.AtlasId,
                    DonorType = updatedDonor.DonorType.ToMatchingAlgorithmType(),
                    A_1 = updatedDonor.A_1,
                    A_2 = updatedDonor.A_2,
                    B_1 = updatedDonor.B_1,
                    B_2 = updatedDonor.B_2,
                    C_1 = updatedDonor.C_1,
                    C_2 = updatedDonor.C_2,
                    DPB1_1 = updatedDonor.DPB1_1,
                    DPB1_2 = updatedDonor.DPB1_2,
                    DQB1_1 = updatedDonor.DQB1_1,
                    DQB1_2 = updatedDonor.DQB1_2,
                    DRB1_1 = updatedDonor.DRB1_1,
                    DRB1_2 = updatedDonor.DRB1_2,
                }
            };
        }
    }
}