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
    public interface IDonorOperationApplier
    {
        Task ApplyDonorOperationBatch(UpdateMode updateMode, IEnumerable<DonorUpdate> donorUpdates);
    }
    
    public class DonorOperationApplier : IDonorOperationApplier
    {
        private readonly IDonorRepository donorRepository;
        private readonly IMessagingServiceBusClient messagingServiceBusClient;

        public DonorOperationApplier(IDonorRepository donorRepository, IMessagingServiceBusClient messagingServiceBusClient)
        {
            this.donorRepository = donorRepository;
            this.messagingServiceBusClient = messagingServiceBusClient;
        }
        
        public async Task ApplyDonorOperationBatch(UpdateMode updateMode, IEnumerable<DonorUpdate> donorUpdates)
        {
            var updatesByType = donorUpdates.GroupBy(du => du.ChangeType);
            foreach (var updateCollection in updatesByType)
            {
                switch (updateCollection.Key)
                {
                    case ChangeType.Create:
                        await donorRepository.InsertDonorBatch(updateCollection.Select(MapToDatabaseDonor));
                        break;
                    case ChangeType.Delete:
                        throw new NotImplementedException();
                    case ChangeType.Update:
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // The process of publishing and consuming update messages for the matching algorithm is very slow. 
                // For an initial load, donors will be imported in bulk to the matching algorithm, via a manually triggered process 
                if (updateMode != UpdateMode.Initial)
                {
                    foreach (var update in updateCollection)
                    {
                        await messagingServiceBusClient.PublishDonorUpdateMessage(MapToMatchingUpdateMessage(update));
                    }
                }
            }
        }

        private static Donor MapToDatabaseDonor(DonorUpdate fileUpdate)
        {
            var donor = new Donor
            {
                DonorId = fileUpdate.RecordId.ToString(),
                DonorType = fileUpdate.DonorType.ToDatabaseType(),
                EthnicityCode = fileUpdate.Ethnicity,
                RegistryCode = fileUpdate.RegistryCode.ToString(),
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

        private static SearchableDonorUpdate MapToMatchingUpdateMessage(DonorUpdate fileUpdate)
        {
            return new SearchableDonorUpdate
            {
                AuditId = 0,
                DonorId = fileUpdate.RecordId.ToString(),
                PublishedDateTime = DateTimeOffset.UtcNow,
                IsAvailableForSearch = fileUpdate.ChangeType != ChangeType.Delete,
                SearchableDonorInformation = new SearchableDonorInformation
                {
                    DonorId = fileUpdate.RecordId,
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