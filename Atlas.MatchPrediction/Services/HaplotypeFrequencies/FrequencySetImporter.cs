using System;
using System.IO;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using MoreLinq.Extensions;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    internal interface IFrequencySetImporter
    {
        Task Import(HaplotypeFrequencySetMetadata metadata, Stream blob);
    }

    internal class FrequencySetImporter : IFrequencySetImporter
    {
        private readonly IFrequencyCsvReader frequenciesStreamReader;
        private readonly IHaplotypeFrequencySetRepository setRepository;
        private readonly IHaplotypeFrequenciesRepository frequenciesRepository;

        public FrequencySetImporter(
            IFrequencyCsvReader frequenciesStreamReader,
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

            ValidateMetaData(metadata);
            var set = await AddNewInactiveSet(metadata);
            await StoreFrequencies(blob, set.Id);
            await setRepository.ActivateSet(set.Id);
        }

        private static void ValidateMetaData(HaplotypeFrequencySetMetadata metadata)
        {
            if (!metadata.Ethnicity.IsNullOrEmpty() && metadata.Registry.IsNullOrEmpty())
            {
                throw new ArgumentException($"Cannot import set: Ethnicity ('{metadata.Ethnicity}') provided but no registry.");
            }
        }

        private async Task<HaplotypeFrequencySet> AddNewInactiveSet(HaplotypeFrequencySetMetadata metadata)
        {
            var newSet = new HaplotypeFrequencySet
            {
                RegistryCode = metadata.Registry,
                EthnicityCode = metadata.Ethnicity,
                Active = false,
                Name = metadata.Name,
                DateTimeAdded = DateTimeOffset.Now
            };

            return await setRepository.AddSet(newSet);
        }

        private async Task StoreFrequencies(Stream stream, int setId)
        {
            const int batchSize = 10000;
            var frequencies = frequenciesStreamReader.GetFrequencies(stream);

            // Cannot check if full frequency list has any entries without enumerating it, so we must check when processing rather than up-front
            var hasImportedAnyFrequencies = false;
            
            foreach (var frequencyBatch in frequencies.Batch(batchSize))
            {
                await frequenciesRepository.AddHaplotypeFrequencies(setId, frequencyBatch);
                hasImportedAnyFrequencies = true;
            }
            
            if (!hasImportedAnyFrequencies)
            {
                throw new Exception("No haplotype frequencies provided");
            }
        }
    }
}
