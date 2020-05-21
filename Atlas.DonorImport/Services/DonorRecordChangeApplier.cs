using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Models.FileSchema;

namespace Atlas.DonorImport.Services
{
    internal interface IDonorRecordChangeApplier
    {
        // ReSharper disable once ParameterTypeCanBeEnumerable.Global
        Task ApplyDonorOperationBatch(IReadOnlyCollection<DonorUpdate> donorUpdates);
    }
    
    internal class DonorRecordChangeApplier : IDonorRecordChangeApplier
    {
        private readonly IDonorRepository donorRepository;

        public DonorRecordChangeApplier(IDonorRepository donorRepository)
        {
            this.donorRepository = donorRepository;
        }
        
        public async Task ApplyDonorOperationBatch(IReadOnlyCollection<DonorUpdate> donorUpdates)
        {
            var updatesByType = donorUpdates.GroupBy(du => du.ChangeType);
            foreach (var updatesOfSameOperationType in updatesByType)
            {
                switch (updatesOfSameOperationType.Key)
                {
                    case ImportDonorChangeType.Create:
                        await donorRepository.InsertDonorBatch(updatesOfSameOperationType.Select(MapDonor));
                        break;
                    case ImportDonorChangeType.Delete:
                        throw new NotImplementedException();
                    case ImportDonorChangeType.Update:
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static Donor MapDonor(DonorUpdate fileUpdate)
        {
            return new Donor
            {
                DonorId = fileUpdate.RecordId,
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
                // TODO: ATLAS-167: Actually calculate hash
                Hash = ""
            };
        }
    }
}