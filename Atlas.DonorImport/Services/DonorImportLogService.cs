using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Models.FileSchema;

namespace Atlas.DonorImport.Services
{
    internal interface IDonorImportLogService
    {
        public Task<IEnumerable<DonorUpdate>> FilterDonorUpdatesBasedOnUpdateTime(IEnumerable<DonorUpdate> donorUpdates, DateTime uploadTime);
        public Task SetLastUpdated(DonorUpdate donorUpdate, DateTime lastUpdated);
        public Task SetLastUpdatedByBatch(List<DonorUpdate> donorBatch, DateTime lastUpdateTime);
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
            return await donorUpdates.WhereAsync(async du => await ShouldUpdateDonor(du, uploadTime));
        }

        public async Task SetLastUpdated(DonorUpdate donorUpdate, DateTime lastUpdated)
        {
            await repository.SetLastUpdated(donorUpdate.RecordId, lastUpdated);
        }

        public async Task SetLastUpdatedByBatch(List<DonorUpdate> donorBatch, DateTime lastUpdateTime)
        {
            var donorIdBatch = donorBatch.Select(d => d.RecordId);
            await repository.SetLastUpdatedByBatch(donorIdBatch, lastUpdateTime);
        }

        private async Task<bool> ShouldUpdateDonor(DonorUpdate donorUpdate, DateTime uploadTime)
        {
            var date = await repository.GetLastUpdateForDonorWithId(donorUpdate.RecordId);
            return uploadTime >= date;
        }
    }
}