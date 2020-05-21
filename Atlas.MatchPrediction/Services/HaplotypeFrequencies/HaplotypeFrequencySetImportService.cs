using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    public interface IHaplotypeFrequencySetImportService
    {
        Task Import(HaplotypeFrequencySetMetaData metaData, Stream blob);
    }

    public class HaplotypeFrequencySetImportService : IHaplotypeFrequencySetImportService
    {
        private readonly IHaplotypeFrequenciesStreamReader frequenciesStreamReader;
        private readonly IHaplotypeFrequencySetRepository setRepository;
        private readonly IHaplotypeFrequenciesRepository frequenciesRepository;

        public HaplotypeFrequencySetImportService(
            IHaplotypeFrequenciesStreamReader frequenciesStreamReader,
            IHaplotypeFrequencySetRepository setRepository,
            IHaplotypeFrequenciesRepository frequenciesRepository)
        {
            this.frequenciesStreamReader = frequenciesStreamReader;
            this.setRepository = setRepository;
            this.frequenciesRepository = frequenciesRepository;
        }

        public async Task Import(HaplotypeFrequencySetMetaData metaData, Stream blob)
        {
            if (metaData == null || blob == null)
            {
                throw new ArgumentNullException();
            }

            var set = await AddSet(metaData);

            await StoreFrequencies(blob, set);
        }

        private async Task<HaplotypeFrequencySet> AddSet(HaplotypeFrequencySetMetaData metaData)
        {
            ValidateMetaData(metaData);
            await DeactivateActiveSetIfExists(metaData);
            return await AddNewActiveSet(metaData);
        }

        private static void ValidateMetaData(HaplotypeFrequencySetMetaData metaData)
        {
            if (!metaData.Ethnicity.IsNullOrEmpty() && metaData.Registry.IsNullOrEmpty())
            {
                throw new ArgumentException($"Cannot import set: Ethnicity ('{metaData.Ethnicity}') provided but no registry.");
            }
        }

        private async Task DeactivateActiveSetIfExists(HaplotypeFrequencySetMetaData metaData)
        {
           var existingSet = await setRepository.GetActiveSet(metaData.Registry, metaData.Ethnicity);

           if (existingSet == null)
           {
               return;
           }

           await setRepository.DeactivateSet(existingSet);
        }

        private async Task<HaplotypeFrequencySet> AddNewActiveSet(HaplotypeFrequencySetMetaData metaData)
        {
            var newSet = new HaplotypeFrequencySet
            {
                Registry = metaData.Registry,
                Ethnicity = metaData.Ethnicity,
                Active = true,
                Name = metaData.Name,
                DateTimeAdded = DateTimeOffset.Now
            };

            return await setRepository.AddSet(newSet);
        }

        private async Task StoreFrequencies(Stream blob, HaplotypeFrequencySet set)
        {
            const int batchSize = 1000;
            var startFrom = 0;
            var frequencies = frequenciesStreamReader.GetFrequencies(blob, batchSize, startFrom).ToList();

            if (!frequencies.Any())
            {
                throw new Exception("No haplotype frequencies could be read from the file.");
            }

            while (frequencies.Any())
            {
                await frequenciesRepository.AddHaplotypeFrequencies(set.Id, frequencies);
                startFrom += frequencies.Count;
                frequencies = frequenciesStreamReader.GetFrequencies(blob, batchSize, startFrom).ToList();
            }
        }
    }
}
