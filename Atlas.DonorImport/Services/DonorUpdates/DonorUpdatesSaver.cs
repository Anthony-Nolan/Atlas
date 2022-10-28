using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.DonorImport.Services.DonorUpdates
{
    public interface IDonorUpdatesSaver
    {
        Task Save(IReadOnlyCollection<SearchableDonorUpdate> donorUpdates);
    }

    internal class DonorUpdatesSaver : IDonorUpdatesSaver
    {
        private readonly IPublishableDonorUpdatesRepository updatesRepository;

        public DonorUpdatesSaver(IPublishableDonorUpdatesRepository updatesRepository)
        {
            this.updatesRepository = updatesRepository;
        }

        public async Task Save(IReadOnlyCollection<SearchableDonorUpdate> donorUpdates)
        {
            if (donorUpdates.IsNullOrEmpty())
            {
                return;
            }

            var publishableUpdates = donorUpdates.Select(u => u.ToPublishableDonorUpdate()).ToList();
            await updatesRepository.BulkInsert(publishableUpdates);
        }
    }
}
