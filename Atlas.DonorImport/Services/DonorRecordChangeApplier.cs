using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.DonorImport.Services
{
    internal interface IDonorRecordChangeApplier
    {
        // ReSharper disable once ParameterTypeCanBeEnumerable.Global
        Task ApplyDonorRecordChangeBatch(IReadOnlyCollection<DonorUpdate> donorUpdates);
    }

    internal class DonorRecordChangeApplier : IDonorRecordChangeApplier
    {
        private readonly IDonorImportRepository donorImportRepository;
        private readonly IMessagingServiceBusClient messagingServiceBusClient;

        public DonorRecordChangeApplier(IDonorImportRepository donorImportRepository, IMessagingServiceBusClient messagingServiceBusClient)
        {
            this.donorImportRepository = donorImportRepository;
            this.messagingServiceBusClient = messagingServiceBusClient;
        }

        public async Task ApplyDonorRecordChangeBatch(IReadOnlyCollection<DonorUpdate> donorUpdates)
        {
            var updatesByType = donorUpdates.GroupBy(du => du.ChangeType);
            foreach (var updatesOfSameOperationType in updatesByType)
            {
                switch (updatesOfSameOperationType.Key)
                {
                    case ImportDonorChangeType.Create:
                        await donorImportRepository.InsertDonorBatch(updatesOfSameOperationType.Select(MapToDatabaseDonor));
                        break;
                    case ImportDonorChangeType.Delete:
                        throw new NotImplementedException();
                    case ImportDonorChangeType.Update:
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }


                var shouldFetchAtlasDonorIds = updatesOfSameOperationType.Key != ImportDonorChangeType.Delete &&
                                               updatesOfSameOperationType.Any(u => u.UpdateMode != UpdateMode.Full);
                var atlasDonors = shouldFetchAtlasDonorIds
                    ? await donorRepository.GetDonorsByExternalDonorCodes(updatesOfSameOperationType.Select(u => u.RecordId))
                    : new Dictionary<string, Donor>();

                // The process of publishing and consuming update messages for the matching algorithm is very slow. 
                // For an initial load, donors will be imported in bulk to the matching algorithm, via a manually triggered process 
                foreach (var update in updatesOfSameOperationType)
                {
                    if (update.UpdateMode != UpdateMode.Full)
                    {
                        var atlasDonor = atlasDonors[update.RecordId];
                        if (atlasDonor == null)
                        {
                            throw new Exception($"Could not fnd created/updated donor in Atlas database: {update.RecordId}");
                        }
                        var atlasId = atlasDonor.AtlasId;
                        await messagingServiceBusClient.PublishDonorUpdateMessage(MapToMatchingUpdateMessage(update, atlasId));
                    }
                }
            }
        }

        private static Donor MapToDatabaseDonor(DonorUpdate fileUpdate)
        {
            var donor = new Donor
            {
                ExternalDonorCode = fileUpdate.RecordId,
                DonorType = fileUpdate.DonorType.ToDatabaseType(),
                EthnicityCode = fileUpdate.Ethnicity,
                RegistryCode = fileUpdate.RegistryCode,
                A_1 = fileUpdate.Hla.A.Field1,
                A_2 = fileUpdate.Hla.A.Field2,
                B_1 = fileUpdate.Hla.B.Field1,
                B_2 = fileUpdate.Hla.B.Field2,
                C_1 = fileUpdate.Hla.C.Field1,
                C_2 = fileUpdate.Hla.C.Field2,
                DPB1_1 = fileUpdate.Hla.DPB1.Field1,
                DPB1_2 = fileUpdate.Hla.DPB1.Field2,
                DQB1_1 = fileUpdate.Hla.DQB1.Field1,
                DQB1_2 = fileUpdate.Hla.DQB1.Field2,
                DRB1_1 = fileUpdate.Hla.DRB1.Field1,
                DRB1_2 = fileUpdate.Hla.DRB1.Field2,
            };
            donor.Hash = donor.CalculateHash();
            return donor;
        }

        private static SearchableDonorUpdate MapToMatchingUpdateMessage(DonorUpdate fileUpdate, int atlasId)
        {
            return new SearchableDonorUpdate
            {
                AuditId = 0,
                DonorId = atlasId,
                PublishedDateTime = DateTimeOffset.UtcNow,
                IsAvailableForSearch = fileUpdate.ChangeType != ImportDonorChangeType.Delete,
                SearchableDonorInformation = new SearchableDonorInformation
                {
                    DonorId = atlasId,
                    DonorType = fileUpdate.DonorType.ToMatchingAlgorithmType().ToString(),
                    A_1 = fileUpdate.Hla.A.Field1,
                    A_2 = fileUpdate.Hla.A.Field2,
                    B_1 = fileUpdate.Hla.B.Field1,
                    B_2 = fileUpdate.Hla.B.Field2,
                    C_1 = fileUpdate.Hla.C.Field1,
                    C_2 = fileUpdate.Hla.C.Field2,
                    DPB1_1 = fileUpdate.Hla.DPB1.Field1,
                    DPB1_2 = fileUpdate.Hla.DPB1.Field2,
                    DQB1_1 = fileUpdate.Hla.DQB1.Field1,
                    DQB1_2 = fileUpdate.Hla.DQB1.Field2,
                    DRB1_1 = fileUpdate.Hla.DRB1.Field1,
                    DRB1_2 = fileUpdate.Hla.DRB1.Field2,
                }
            };
        }
    }
}