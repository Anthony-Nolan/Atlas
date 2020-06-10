using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Models.Mapping;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.DonorImport.Services
{
    internal interface IDonorRecordChangeApplier
    {
        // ReSharper disable once ParameterTypeCanBeEnumerable.Global
        Task ApplyDonorRecordChangeBatch(IReadOnlyCollection<DonorUpdate> donorUpdates, string fileName);
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

        public async Task ApplyDonorRecordChangeBatch(IReadOnlyCollection<DonorUpdate> donorUpdates, string fileName)
        {
            var updatesByType = donorUpdates.GroupBy(du => du.ChangeType);
            foreach (var updatesOfSameOperationType in updatesByType)
            {
                switch (updatesOfSameOperationType.Key)
                {
                    case ImportDonorChangeType.Create:
                        var creations = updatesOfSameOperationType.Select(update => MapToDatabaseDonor(update, fileName)).ToList();
                        await donorImportRepository.InsertDonorBatch(creations);
                        break;
                    case ImportDonorChangeType.Delete:
                        throw new NotImplementedException();
                    case ImportDonorChangeType.Update:
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var shouldFetchAtlasDonorIds = updatesOfSameOperationType.Any(u => u.UpdateMode != UpdateMode.Full);
                var atlasDonors = shouldFetchAtlasDonorIds
                    ? await donorInspectionRepository.GetDonorsByExternalDonorCodes(updatesOfSameOperationType.Select(u => u.RecordId))
                    : new Dictionary<string, Donor>();

                // The process of publishing and consuming update messages for the matching algorithm is very slow. 
                // For an initial load, donors will be imported in bulk to the matching algorithm, via a manually triggered process 
                foreach (var update in updatesOfSameOperationType)
                {
                    if (update.UpdateMode != UpdateMode.Full)
                    {
                        var atlasDonor = atlasDonors[update.RecordId];
                        SearchableDonorUpdate updateMessage;

                        if (update.ChangeType == ImportDonorChangeType.Delete)
                        {
                            updateMessage = MapToDeletionUpdate(atlasDonor);
                        }
                        else
                        {
                            if (atlasDonor == null)
                            {
                                throw new Exception($"Could not find created/updated donor in Atlas database: {update.RecordId}");
                            }

                            updateMessage = MapToMatchingUpdateMessage(atlasDonor);
                        }

                        await messagingServiceBusClient.PublishDonorUpdateMessage(updateMessage);
                    }
                }
            }
        }

        private Donor MapToDatabaseDonor(DonorUpdate fileUpdate, string fileName)
        {
            Dictionary<string, string> createLogContext(Locus locus) =>
                new Dictionary<string, string>
                {
                    {"ImportFile", fileName},
                    {"DonorCode", fileUpdate.RecordId},
                    {"Locus", locus.ToString()}
                };

            var interpretedA = locusInterpreter.Interpret(fileUpdate.Hla.A, createLogContext(Locus.A));
            var interpretedB = locusInterpreter.Interpret(fileUpdate.Hla.B, createLogContext(Locus.B));
            var interpretedC = locusInterpreter.Interpret(fileUpdate.Hla.C, createLogContext(Locus.C));
            var interpretedDpb1 = locusInterpreter.Interpret(fileUpdate.Hla.DPB1, createLogContext(Locus.Dpb1));
            var interpretedDqb1 = locusInterpreter.Interpret(fileUpdate.Hla.DQB1, createLogContext(Locus.Dqb1));
            var interpretedDrb1 = locusInterpreter.Interpret(fileUpdate.Hla.DRB1, createLogContext(Locus.Drb1));
            
            var donor = new Donor
            {
                ExternalDonorCode = fileUpdate.RecordId,
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

        private SearchableDonorUpdate MapToDeletionUpdate(Donor updatedDonor)
        {
            return new SearchableDonorUpdate
            {
                DonorId = updatedDonor.AtlasId,
                IsAvailableForSearch = false,
                SearchableDonorInformation = null
            };
        }

        private SearchableDonorUpdate MapToMatchingUpdateMessage(Donor updatedDonor)
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