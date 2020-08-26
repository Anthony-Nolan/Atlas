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
        public IAsyncEnumerable<DonorUpdate> FilterDonorUpdatesBasedOnUpdateTime(IEnumerable<DonorUpdate> donorUpdates, DateTime uploadTime);
        public Task SetLastUpdated(DateTime lastUpdated, IReadOnlyCollection<DonorUpdate> updates);
    }

    internal class DonorImportLogService : IDonorImportLogService
    {
        private readonly IDonorImportLogRepository repository;

        public DonorImportLogService(IDonorImportLogRepository repository)
        {
            this.repository = repository;
        }

        public async IAsyncEnumerable<DonorUpdate> FilterDonorUpdatesBasedOnUpdateTime(IEnumerable<DonorUpdate> donorUpdates, DateTime uploadTime)
        {
            foreach (var update in donorUpdates)
            {
                if (await ShouldUpdateDonor(update, uploadTime))
                {
                    yield return update;
                }
            }
        }

        public async Task SetLastUpdated(DateTime lastUpdated, IReadOnlyCollection<DonorUpdate> updates)
        {
            var (deletions, upserts) = updates.ReifyAndSplit(u => u.ChangeType == ImportDonorChangeType.Delete);

            await repository.DeleteDonorLogBatch(deletions.Select(u => u.RecordId).ToList());
            await repository.SetLastUpdatedBatch(upserts.Select(u => u.RecordId), lastUpdated);
        }

        private async Task<bool> ShouldUpdateDonor(DonorUpdate donorUpdate, DateTime uploadTime)
        {
            var donorExists = await repository.CheckDonorExists(donorUpdate.RecordId);
            if (donorExists && donorUpdate.ChangeType == ImportDonorChangeType.Create)
            {
                throw new DuplicateDonorImportException(
                    $"Attempted to create a donor that already existed. External Donor Code: {donorUpdate.RecordId}");
            }

            var date = await repository.GetLastUpdateForDonorWithId(donorUpdate.RecordId);
            return uploadTime >= date;
        }
    }
}