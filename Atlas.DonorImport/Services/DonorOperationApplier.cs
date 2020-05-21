using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Models.FileSchema;

namespace Atlas.DonorImport.Services
{
    public interface IDonorOperationApplier
    {
        Task ApplyDonorOperationBatch(IEnumerable<DonorUpdate> donorUpdates);
    }
    
    public class DonorOperationApplier : IDonorOperationApplier
    {
        private readonly IDonorRepository donorRepository;

        public DonorOperationApplier(IDonorRepository donorRepository)
        {
            this.donorRepository = donorRepository;
        }
        
        public async Task ApplyDonorOperationBatch(IEnumerable<DonorUpdate> donorUpdates)
        {
            var updatesByType = donorUpdates.GroupBy(du => du.ChangeType);
            foreach (var updateCollection in updatesByType)
            {
                switch (updateCollection.Key)
                {
                    case ChangeType.Create:
                        await donorRepository.InsertDonorBatch(updateCollection.Select(MapDonor));
                        break;
                    case ChangeType.Delete:
                        throw new NotImplementedException();
                    case ChangeType.Update:
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static Donor MapDonor(DonorUpdate fileUpdate)
        {
            var donor = new Donor
            {
                DonorId = fileUpdate.RecordId,
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
    }
}