using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    public interface IHaplotypeFrequencySetImportService
    {
        Task Import(HaplotypeFrequencySetMetadata metadata, Stream blob);
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

        public async Task Import(HaplotypeFrequencySetMetadata metadata, Stream blob)
        {
            if (metadata == null || blob == null)
            {
                throw new ArgumentNullException();
            }

            var set = await AddSet(metadata);

            await StoreFrequencies(blob, set);
        }

        private async Task<HaplotypeFrequencySet> AddSet(HaplotypeFrequencySetMetadata metadata)
        {
            ValidateMetaData(metadata);
            await DeactivateActiveSetIfExists(metadata);
            return await AddNewActiveSet(metadata);
        }

        private static void ValidateMetaData(HaplotypeFrequencySetMetadata metadata)
        {
            if (!metadata.Ethnicity.IsNullOrEmpty() && metadata.Registry.IsNullOrEmpty())
            {
                throw new ArgumentException($"Cannot import set: Ethnicity ('{metadata.Ethnicity}') provided but no registry.");
            }
        }

        private async Task DeactivateActiveSetIfExists(HaplotypeFrequencySetMetadata metadata)
        {
           var existingSet = await setRepository.GetActiveSet(metadata.Registry, metadata.Ethnicity);

           if (existingSet == null)
           {
               return;
           }

           await setRepository.DeactivateSet(existingSet);
        }

        private async Task<HaplotypeFrequencySet> AddNewActiveSet(HaplotypeFrequencySetMetadata metadata)
        {
            var newSet = new HaplotypeFrequencySet
            {
                Registry = metadata.Registry,
                Ethnicity = metadata.Ethnicity,
                Active = true,
                Name = metadata.Name,
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
