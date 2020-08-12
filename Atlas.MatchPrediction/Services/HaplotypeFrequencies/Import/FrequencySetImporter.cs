using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions;
using Atlas.MatchPrediction.Utils;
using TaskExtensions = Atlas.Common.Utils.Tasks.TaskExtensions;

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
        private readonly IHlaCategorisationService hlaCategorisationService;
        private readonly ILogger logger;
        private readonly MatchPredictionImportSettings matchPredictionImportSettings;

        public FrequencySetImporter(
            IFrequencySetMetadataExtractor metadataExtractor,
            IFrequencyCsvReader frequencyCsvReader,
            IHaplotypeFrequencySetRepository setRepository,
            IHaplotypeFrequenciesRepository frequenciesRepository,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            IHlaCategorisationService hlaCategorisationService,
            ILogger logger,
            MatchPredictionImportSettings matchPredictionImportSettings)
        {
            this.metadataExtractor = metadataExtractor;
            this.frequencyCsvReader = frequencyCsvReader;
            this.setRepository = setRepository;
            this.frequenciesRepository = frequenciesRepository;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
            this.hlaCategorisationService = hlaCategorisationService;
            this.logger = logger;
            this.matchPredictionImportSettings = matchPredictionImportSettings;
        }

        public async Task Import(FrequencySetFile file, bool convertToPGroups)
        {
            if (file.Contents == null)
            {
                throw new EmptyHaplotypeFileException();
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
                throw new InvalidFilePathException($"Cannot import set: Ethnicity ('{metadata.Ethnicity}') provided but no registry.");
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
            var gGroupHaplotypes = ReadGGroupHaplotypeFrequencies(stream);

            var haplotypes = gGroupHaplotypes.Select(r => r.Hla).ToList();

            if (haplotypes.Count != haplotypes.Distinct().Count())
            {
                throw new DuplicateHaplotypeImportException();
            }

            var allHlaGGroup = haplotypes
                .Select(l => 
                    l.AllAtLoci(h => 
                        hlaCategorisationService.ConformsToSpecificHlaFormat(h, HlaTypingCategory.GGroup), LocusSettings.MatchPredictionLoci))
                .All(x => x);

            if (!allHlaGGroup)
            {
                throw new MalformedHaplotypeFileException("Haplotype Hla must be of type GGroup.");
            }

            if (!gGroupHaplotypes.Any())
            {
                throw new EmptyHaplotypeFileException();
            }

            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(matchPredictionImportSettings.HlaNomenclatureVersion);

            var haplotypesToStore = convertToPGroups
                ? await ConvertHaplotypesToPGroupResolutionAndConsolidate(gGroupHaplotypes, hlaMetadataDictionary)
                : gGroupHaplotypes;

            await frequenciesRepository.AddHaplotypeFrequencies(setId, haplotypesToStore);
        }

        private IReadOnlyCollection<HaplotypeFrequency> ReadGGroupHaplotypeFrequencies(Stream stream)
        {
            return frequencyCsvReader.GetFrequencies(stream).Select(f => new HaplotypeFrequency
            {
                A = f.A,
                B = f.B,
                C = f.C,
                DQB1 = f.Dqb1,
                DRB1 = f.Drb1,
                Frequency = f.Frequency,
                TypingCategory = HaplotypeTypingCategory.GGroup
            }).ToList();
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