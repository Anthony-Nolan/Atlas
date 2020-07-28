using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Utils;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import
{
    public interface IFrequencySetImporter
    {
        Task Import(FrequencySetFile file, bool convertToPGroups = true);
    }

    internal class FrequencySetImporter : IFrequencySetImporter
    {
        private readonly IFrequencySetMetadataExtractor metadataExtractor;
        private readonly IFrequencyCsvReader frequencyCsvReader;
        private readonly IHaplotypeFrequencySetRepository setRepository;
        private readonly IHaplotypeFrequenciesRepository frequenciesRepository;
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;

        public FrequencySetImporter(
            IFrequencySetMetadataExtractor metadataExtractor,
            IFrequencyCsvReader frequencyCsvReader,
            IHaplotypeFrequencySetRepository setRepository,
            IHaplotypeFrequenciesRepository frequenciesRepository,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory)
        {
            this.metadataExtractor = metadataExtractor;
            this.frequencyCsvReader = frequencyCsvReader;
            this.setRepository = setRepository;
            this.frequenciesRepository = frequenciesRepository;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
        }

        public async Task Import(FrequencySetFile file, bool convertToPGroups)
        {
            if (file.FullPath.IsNullOrEmpty() || file.Contents == null)
            {
                throw new ArgumentNullException();
            }

            var metadata = GetMetadata(file);
            var set = await AddNewInactiveSet(metadata);
            await StoreFrequencies(file.Contents, set.Id, convertToPGroups);
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

        private async Task StoreFrequencies(Stream stream, int setId, bool convertToPGroups)
        {
            // Load all frequencies into memory, to perform aggregation by PGroup.
            // Largest known HF set is ~300,000 entries, which is reasonable to load into memory here.
            var frequencies = frequencyCsvReader.GetFrequencies(stream).ToList();

            if (!frequencies.Any())
            {
                throw new Exception("No haplotype frequencies provided");
            }

            // TODO: ATLAS-600: Read HLA nomenclature version from file data, rather than hard-coding
            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary("3400");

            var convertedHaplotypes = convertToPGroups
                ? await ConvertHaplotypesToPGroupResolutionAndConsolidate(frequencies, hlaMetadataDictionary)
                : frequencies.Select(f =>
                {
                    f.TypingCategory = HaplotypeTypingCategory.GGroup;
                    return f;
                });
            await frequenciesRepository.AddHaplotypeFrequencies(setId, convertedHaplotypes);
        }

        private static async Task<IEnumerable<HaplotypeFrequency>> ConvertHaplotypesToPGroupResolutionAndConsolidate(
            IEnumerable<HaplotypeFrequency> frequencyBatch,
            IHlaMetadataDictionary hlaMetadataDictionary)
        {
            var convertedFrequencies = await Task.WhenAll(frequencyBatch.Select(async haplotypeFrequency =>
            {
                var pGroupTyped = await hlaMetadataDictionary.ConvertGGroupsToPGroups(haplotypeFrequency.Hla, LocusSettings.MatchPredictionLoci);

                if (!pGroupTyped.AnyAtLoci(x => x == null, LocusSettings.MatchPredictionLoci))
                {
                    haplotypeFrequency.Hla = pGroupTyped;
                    haplotypeFrequency.TypingCategory = HaplotypeTypingCategory.PGroup;
                }
                else
                {
                    haplotypeFrequency.TypingCategory = HaplotypeTypingCategory.GGroup;
                }

                return haplotypeFrequency;
            }));

            var combined = convertedFrequencies
                .GroupBy(f => f.Hla)
                .Select(groupByHla =>
                {
                    // arbitrary haplotype frequency object, as everything but the frequency will be the same in all cases 
                    var frequency = groupByHla.First();
                    frequency.Frequency = groupByHla.OrderBy(x => x.Frequency).Sum(x => x.Frequency);
                    return frequency;
                });
            return combined;
        }
    }
}