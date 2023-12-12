using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Helpers;
using Atlas.DonorImport.Models;
using MoreLinq;

namespace Atlas.DonorImport.Services.DonorUpdates
{
    public interface IDonorUpdatesSaver
    {
        Task Save(IEnumerable<SearchableDonorUpdate> donorUpdates);
        Task GenerateAndSave(IEnumerable<int> donorIds);
    }

    internal class DonorUpdatesSaver : IDonorUpdatesSaver
    {
        private readonly IPublishableDonorUpdatesRepository updatesRepository;
        private readonly IDonorReadRepository donorReadRepository;


        public DonorUpdatesSaver(IPublishableDonorUpdatesRepository updatesRepository, IDonorReadRepository donorReadRepository)
        {
            this.updatesRepository = updatesRepository;
            this.donorReadRepository = donorReadRepository;
        }

        public async Task Save(IEnumerable<SearchableDonorUpdate> donorUpdates)
        {
            if (donorUpdates.IsNullOrEmpty())
            {
                return;
            }

            var publishableUpdates = donorUpdates.Select(u => u.ToPublishableDonorUpdate()).ToList();
            await updatesRepository.BulkInsert(publishableUpdates);
        }

        public async Task GenerateAndSave(IEnumerable<int> donorIds)
        {
            var donorInfo = await donorReadRepository.GetDonorsByIds(donorIds);
            var updates = donorIds.Select(id => Convert(id, donorInfo));

            await Save(updates);
        }

        private SearchableDonorUpdate Convert(int id, IReadOnlyDictionary<int, Data.Models.Donor> donors)
        {
            return donors.TryGetValue(id, out var donor)
                ? SearchableDonorUpdateMapper.MapToMatchingUpdateMessage(donor)
                : SearchableDonorUpdateMapper.MapToDeletionUpdateMessage(id);
        }
    }
}
