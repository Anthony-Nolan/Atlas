using Atlas.Common.ApplicationInsights;
using Atlas.DonorImport.Data.Migrations;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Helpers;
using Atlas.DonorImport.Services.DonorUpdates;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.DonorImport.Services
{
    public interface IManuallyPublishDonorUpdatesService
    {
        Task<bool> PublishDonorUpdates(int[] ids);
    }


    public class ManuallyPublishDonorUpdatesService : IManuallyPublishDonorUpdatesService
    {
        private const int SaveBatchSize = 1500;

        private readonly IDonorReadRepository donorReadRepository;
        private readonly IDonorUpdatesSaver donorUpdatesSaver;


        public ManuallyPublishDonorUpdatesService(IDonorReadRepository donorReadRepository, IDonorUpdatesSaver donorUpdatesSaver)
        {
            this.donorReadRepository = donorReadRepository;
            this.donorUpdatesSaver = donorUpdatesSaver;
        }

        public async Task<bool> PublishDonorUpdates(int[] ids)
        {
            try
            {
                var donorInfo = await donorReadRepository.GetDonorsByIds(ids);
                var updates = ids.Select(id => Convert(id, donorInfo)).ToList();

                foreach (var batch in SplitIntoBatches(updates, SaveBatchSize))
                {
                    await donorUpdatesSaver.Save(batch);
                }

                return true;
            }
            catch (Exception ex)
            {
                // TODO: Log error

                return false;
            }
        }

        private SearchableDonorUpdate Convert(int id, IReadOnlyDictionary<int, Data.Models.Donor> donors)
        {
            return donors.TryGetValue(id, out var donor) 
                ? SearchableDonorUpdateHelper.MapToMatchingUpdateMessage(donor) 
                : SearchableDonorUpdateHelper.MapToDeletionUpdateMessage(id);
        }

        private static IEnumerable<List<T>> SplitIntoBatches<T>(IEnumerable<T> input, int batchSize)
        {
            var counter = 0;
            var accumulator = new List<T>();
            foreach (var item in input)
            {
                if (counter == batchSize)
                {
                    yield return accumulator;
                    accumulator = new();
                }

                accumulator.Add(item);
                counter++;
            }

            if (counter > 0)
            {
                yield return accumulator;
            }
        }

    }
}
