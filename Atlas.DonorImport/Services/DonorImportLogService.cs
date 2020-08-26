using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.Models.FileSchema;
using MoreLinq;

namespace Atlas.DonorImport.Services
{
    internal interface IDonorImportLogService
    {
        public IAsyncEnumerable<DonorUpdate> FilterDonorUpdatesBasedOnUpdateTime(IEnumerable<DonorUpdate> donorUpdates, DateTime uploadTime);
        public Task SetLastUpdated(IReadOnlyCollection<DonorUpdate> updates, DateTime lastUpdated);
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
            foreach (var updateBatch in donorUpdates.Batch(2000))
            {
                var reifiedDonorBatch = updateBatch.ToList();
                var updateDates = await repository.GetLastUpdatedTimes(reifiedDonorBatch.Select(u => u.RecordId).ToList());

                foreach (var update in reifiedDonorBatch)
                {
                    var externalDonorCode = update.RecordId;
                    var donorExists = updateDates.ContainsKey(externalDonorCode);
                    if (donorExists && update.ChangeType == ImportDonorChangeType.Create)
                    {
                        throw new DuplicateDonorImportException(
                            $"Attempted to create a donor that already existed. External Donor Code: {externalDonorCode}");
                    }

                    if (updateDates.TryGetValue(externalDonorCode, out var lastUpdateTime))
                    {
                        if (lastUpdateTime >= uploadTime)
                        {
                            yield break;
                        }
                    }

                    yield return update;
                }
            }
        }

        public async Task SetLastUpdated(IReadOnlyCollection<DonorUpdate> updates, DateTime lastUpdated)
        {
            var (deletions, upserts) = updates.ReifyAndSplit(u => u.ChangeType == ImportDonorChangeType.Delete);

            await repository.DeleteDonorLogBatch(deletions.Select(u => u.RecordId).ToList());
            await repository.SetLastUpdatedBatch(upserts.Select(u => u.RecordId), lastUpdated);
        }
    }
}