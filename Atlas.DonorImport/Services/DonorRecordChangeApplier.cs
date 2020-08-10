using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Models.Mapping;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Donor = Atlas.DonorImport.Data.Models.Donor;

namespace Atlas.DonorImport.Services
{
    internal interface IDonorRecordChangeApplier
    {
        // ReSharper disable once ParameterTypeCanBeEnumerable.Global
        Task ApplyDonorRecordChangeBatch(IReadOnlyCollection<DonorUpdate> donorUpdates, DonorImportFile file);
    }

    internal class DonorRecordChangeApplier : IDonorRecordChangeApplier
    {
        private readonly IMessagingServiceBusClient messagingServiceBusClient;
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IDonorReadRepository donorInspectionRepository;
        private readonly IImportedLocusInterpreter locusInterpreter;
        private readonly ILogger logger;

        public DonorRecordChangeApplier(
            IMessagingServiceBusClient messagingServiceBusClient,
            IDonorImportRepository donorImportRepository,
            IDonorReadRepository donorInspectionRepository,
            IImportedLocusInterpreter locusInterpreter,
            ILogger logger)
        {
            this.donorImportRepository = donorImportRepository;
            this.messagingServiceBusClient = messagingServiceBusClient;
            this.donorInspectionRepository = donorInspectionRepository;
            this.locusInterpreter = locusInterpreter;
            this.logger = logger;
        }

        public async Task ApplyDonorRecordChangeBatch(IReadOnlyCollection<DonorUpdate> donorUpdates, DonorImportFile file)
        {
            var updateMode = DetermineUpdateMode(donorUpdates);

            var updatesByType = donorUpdates.GroupBy(du => du.ChangeType);
            foreach (var updatesOfSameOperationType in updatesByType)
            {
                var externalCodes = updatesOfSameOperationType.Select(update => update.RecordId).ToList();
                switch (updatesOfSameOperationType.Key)
                {
                    case ImportDonorChangeType.Create:
                        await ProcessDonorCreations(updatesOfSameOperationType.ToList(), externalCodes, file.FileLocation, updateMode);
                        break;

                    case ImportDonorChangeType.Edit:
                        await ProcessDonorEdits(updatesOfSameOperationType.ToList(), externalCodes, file);
                        break;

                    case ImportDonorChangeType.Delete:
                        await ProcessDonorDeletions(externalCodes);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private async Task ProcessDonorCreations(
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
                return;
            }

            var newAtlasDonorIds = await donorInspectionRepository.GetDonorIdsByExternalDonorCodes(externalCodes);

            foreach (var creation in creationsWithoutAtlasIds)
            {
                creation.AtlasId = GetAtlasIdFromCode(creation.ExternalDonorCode, newAtlasDonorIds);
                var updateMessage = MapToMatchingUpdateMessage(creation);
                await messagingServiceBusClient.PublishDonorUpdateMessage(updateMessage);
            }
        }

        private async Task ProcessDonorEdits(
            List<DonorUpdate> editUpdates,
            List<string> externalCodes,
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
            }).Where(d => !existingAtlasDonorHashes.Contains(d.Hash)).ToList(); ;

            if (editedDonors.Count > 0)
            {
                await donorImportRepository.UpdateDonorBatch(editedDonors, file.UploadTime);

                var donorEditMessages = editedDonors.Select(MapToMatchingUpdateMessage).ToList();
                await messagingServiceBusClient.PublishDonorUpdateMessages(donorEditMessages);
            }
        }

        private async Task ProcessDonorDeletions(List<string> deletedExternalCodes)
        {
            var deletedAtlasDonorIds = await donorInspectionRepository.GetDonorIdsByExternalDonorCodes(deletedExternalCodes);

            await donorImportRepository.DeleteDonorBatch(deletedAtlasDonorIds.Values.ToList());

            var deletionUpdates = deletedAtlasDonorIds.Values.Select(MapToDeletionUpdateMessage).ToList();
            await messagingServiceBusClient.PublishDonorUpdateMessages(deletionUpdates);
        }

        private int GetAtlasIdFromCode(string donorCode, Dictionary<string, int> codesToIdsDictionary)
        {
            if (!codesToIdsDictionary.TryGetValue(donorCode, out var atlasDonorId))
            {
                throw new Exception($"Could not find expected donor in Atlas database: {donorCode}");
            }

            return atlasDonorId;
        }

        private UpdateMode DetermineUpdateMode(IReadOnlyCollection<DonorUpdate> donorUpdates)
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
        private string LeftTruncateTo256(string fileLocation)
        {
            if (fileLocation.Length > 256)
            {
                return new string(fileLocation.TakeLast(256).ToArray());
            }

            return fileLocation;
        }

        private SearchableDonorUpdate MapToDeletionUpdateMessage(int deletedDonorId)
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