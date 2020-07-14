using System;
using System.IO;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using MoreLinq.Extensions;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import
{
    public interface IFrequencySetImporter
    {
        Task Import(FrequencySetFile file);
    }

    internal class FrequencySetImporter : IFrequencySetImporter
    {
        private readonly IFrequencySetMetadataExtractor metadataExtractor;
        private readonly IFrequencyCsvReader frequencyCsvReader;
        private readonly IHaplotypeFrequencySetRepository setRepository;
        private readonly IHaplotypeFrequenciesRepository frequenciesRepository;

        public FrequencySetImporter(
            IFrequencySetMetadataExtractor metadataExtractor,
            IFrequencyCsvReader frequencyCsvReader,
            IHaplotypeFrequencySetRepository setRepository,
            IHaplotypeFrequenciesRepository frequenciesRepository)
        {
            this.metadataExtractor = metadataExtractor;
            this.frequencyCsvReader = frequencyCsvReader;
            this.setRepository = setRepository;
            this.frequenciesRepository = frequenciesRepository;
        }

        public async Task Import(FrequencySetFile file)
        {
            if (file.FullPath.IsNullOrEmpty() || file.Contents == null)
            {
                throw new ArgumentNullException();
            }

            var metadata = GetMetadata(file);
            var set = await AddNewInactiveSet(metadata);
            await StoreFrequencies(file.Contents, set.Id);
            await setRepository.ActivateSet(set.Id);
        }

        private HaplotypeFrequencySetMetadata GetMetadata(FrequencySetFile file)
        {
            var metadata = metadataExtractor.GetMetadataFromFullPath(file.FullPath);

            if (!metadata.Ethnicity.IsNullOrEmpty() && metadata.Registry.IsNullOrEmpty())
            {
                throw new ArgumentException($"Cannot import set: Ethnicity ('{metadata.Ethnicity}') provided but no registry.");
            }

            return metadata;
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
            var frequencies = frequencyCsvReader.GetFrequencies(stream);

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
