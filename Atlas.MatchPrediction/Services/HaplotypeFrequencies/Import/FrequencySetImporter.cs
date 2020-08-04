using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskExtensions = Atlas.Common.Utils.Tasks.TaskExtensions;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import
{
    public interface IFrequencySetImporter
    {
        /// <summary>
        /// Imports haplotype frequencies from provided stream and stores set using info extracted from blob metadata.
        /// </summary>
        Task ImportFromStream(FrequencySetFile file, bool convertToPGroups = true);

        /// <summary>
        /// Downloads set using provided file name, and then stores haplotype frequencies using the other provided metadata.
        /// </summary>
        Task Import(HaplotypeFrequencySetMetadata fileMetadata, bool convertToPGroups = true);
    }

    internal class FrequencySetImporter : IFrequencySetImporter
    {
        private readonly IFrequencySetMetadataExtractor metadataExtractor;
        private readonly IFrequencySetStreamer setStreamer;
        private readonly IFrequencyCsvReader frequencyCsvReader;
        private readonly IHaplotypeFrequencySetRepository setRepository;
        private readonly IHaplotypeFrequenciesRepository frequenciesRepository;
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private readonly ILogger logger;

        public FrequencySetImporter(
            IFrequencySetMetadataExtractor metadataExtractor,
            IFrequencySetStreamer setStreamer,
            IFrequencyCsvReader frequencyCsvReader,
            IHaplotypeFrequencySetRepository setRepository,
            IHaplotypeFrequenciesRepository frequenciesRepository,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            ILogger logger)
        {
            this.metadataExtractor = metadataExtractor;
            this.setStreamer = setStreamer;
            this.frequencyCsvReader = frequencyCsvReader;
            this.setRepository = setRepository;
            this.frequenciesRepository = frequenciesRepository;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
            this.logger = logger;
        }

        public async Task ImportFromStream(FrequencySetFile file, bool convertToPGroups)
        {
            if (file.FullPath.IsNullOrEmpty() || file.Contents == null)
            {
                throw new ArgumentNullException();
            }

            var metadata = metadataExtractor.GetMetadataFromFullPath(file.FullPath);
            await ImportSet(metadata, file.Contents, convertToPGroups);
        }

        public async Task Import(HaplotypeFrequencySetMetadata fileMetadata, bool convertToPGroups = true)
        {
            if (fileMetadata == null || fileMetadata.Name.IsNullOrEmpty())
            {
                throw new ArgumentNullException();
            }

            var stream = await setStreamer.GetFileContents(fileMetadata.Name);

            await ImportSet(fileMetadata, stream, convertToPGroups);
        }

        private async Task ImportSet(HaplotypeFrequencySetMetadata metadata, Stream fileContents, bool convertToPGroups)
        {
            var set = await AddNewInactiveSet(metadata);
            await StoreFrequencies(fileContents, set.Id, convertToPGroups);
            await setRepository.ActivateSet(set.Id);
        }

        private async Task<HaplotypeFrequencySet> AddNewInactiveSet(HaplotypeFrequencySetMetadata metadata)
        {
            if (metadata == null)
            {
                throw new ArgumentException();
            }

            if (!metadata.EthnicityCode.IsNullOrEmpty() && metadata.RegistryCode.IsNullOrEmpty())
            {
                throw new ArgumentException($"Cannot import set: Ethnicity ('{metadata.EthnicityCode}') provided but no registry.");
            }

            var newSet = new HaplotypeFrequencySet
            {
                RegistryCode = metadata.RegistryCode,
                EthnicityCode = metadata.EthnicityCode,
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

        private async Task<IEnumerable<HaplotypeFrequency>> ConvertHaplotypesToPGroupResolutionAndConsolidate(
            IEnumerable<HaplotypeFrequency> frequencies,
            IHlaMetadataDictionary hlaMetadataDictionary)
        {
            var convertedFrequencies = await logger.RunTimedAsync("Convert frequencies", async () =>
            {
                return await TaskExtensions.WhenEach(frequencies.Select(async frequency =>
                {
                    var pGroupTyped = await hlaMetadataDictionary.ConvertGGroupsToPGroups(frequency.Hla, LocusSettings.MatchPredictionLoci);

                    if (!pGroupTyped.AnyAtLoci(x => x == null, LocusSettings.MatchPredictionLoci))
                    {
                        frequency.Hla = pGroupTyped;
                        frequency.TypingCategory = HaplotypeTypingCategory.PGroup;
                    }
                    else
                    {
                        frequency.TypingCategory = HaplotypeTypingCategory.GGroup;
                    }

                    return frequency;
                }));
            });

            using (logger.RunTimed("Combine frequencies"))
            {
                return convertedFrequencies
                    .GroupBy(f => f.Hla)
                    .Select(groupByHla =>
                    {
                        // arbitrary haplotype frequency object, as everything but the frequency will be the same in all cases 
                        var frequency = groupByHla.First();
                        frequency.Frequency = groupByHla.Select(g => g.Frequency).SumDecimals();
                        return frequency;
                    });
            }
        }
    }
}