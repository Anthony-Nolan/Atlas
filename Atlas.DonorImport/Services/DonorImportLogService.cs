using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Models.FileSchema;

namespace Atlas.DonorImport.Services
{
    internal interface IDonorImportLogService
    {
        public Task<IEnumerable<DonorUpdate>> FilterDonorUpdatesBasedOnUpdateTime(IEnumerable<DonorUpdate> donorUpdates, DateTime uploadTime);
        public Task SetLastUpdated(DonorUpdate donorUpdate, DateTime lastUpdated);
    }
    
    internal class DonorImportLogService : IDonorImportLogService
    {
        private readonly IDonorImportLogRepository repository;

        public DonorImportLogService(IDonorImportLogRepository repository)
        {
            this.repository = repository;
        }
        
        public async Task<IEnumerable<DonorUpdate>> FilterDonorUpdatesBasedOnUpdateTime(IEnumerable<DonorUpdate> donorUpdates, DateTime uploadTime)
        {
            var donorsToUpdate = new List<DonorUpdate>();
            foreach (var donorUpdate in donorUpdates)
            {
                if (await ShouldUpdateDonor(donorUpdate, uploadTime))
                {
                    donorsToUpdate.Add(donorUpdate);
                }
            }

            return donorsToUpdate;
        }

        public async Task SetLastUpdated(DonorUpdate donorUpdate, DateTime lastUpdated)
        {
            await repository.SetLastUpdated(donorUpdate.RecordId, lastUpdated);
        }

        private async Task<bool> ShouldUpdateDonor(DonorUpdate donorUpdate, DateTime uploadTime)
        {
            var date = await repository.GetLastUpdateForDonorWithId(donorUpdate.RecordId);
            if (date == new DateTime())
            {
                return true;
            }
            return uploadTime > date;
        }
    }
}