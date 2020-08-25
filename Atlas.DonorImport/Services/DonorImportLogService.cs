using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Exceptions;
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
            return await donorUpdates.WhereAsync(async du => await ShouldUpdateDonor(du, uploadTime));
        }

        public async Task SetLastUpdated(DonorUpdate donorUpdate, DateTime lastUpdated)
        {
            if (donorUpdate.ChangeType == ImportDonorChangeType.Delete)
            {
                await repository.DeleteDonorLog(donorUpdate.RecordId);
            }
            else
            {
                await repository.SetLastUpdated(donorUpdate.RecordId, lastUpdated);
            }
        }

        private async Task<bool> ShouldUpdateDonor(DonorUpdate donorUpdate, DateTime uploadTime)
        {
            var donorExists = await repository.CheckDonorExists(donorUpdate.RecordId);
            if (donorExists && donorUpdate.ChangeType == ImportDonorChangeType.Create)
            {
                throw new Exception("Tried to Insert a new donor but it already existed.");
            }

            var date = await repository.GetLastUpdateForDonorWithId(donorUpdate.RecordId);
            return uploadTime >= date;
        }
    }
}