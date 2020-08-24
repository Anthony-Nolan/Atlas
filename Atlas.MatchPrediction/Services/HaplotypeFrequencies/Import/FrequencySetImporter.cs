using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
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
        private readonly IFrequencyCsvReader frequencyCsvReader;
        private readonly IHaplotypeFrequencySetRepository setRepository;
        private readonly IHaplotypeFrequenciesRepository frequenciesRepository;
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private readonly ILogger logger;

        public FrequencySetImporter(
            IFrequencyCsvReader frequencyCsvReader,
            IHaplotypeFrequencySetRepository setRepository,
            IHaplotypeFrequenciesRepository frequenciesRepository,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            ILogger logger)
        {
            this.frequencyCsvReader = frequencyCsvReader;
            this.setRepository = setRepository;
            this.frequenciesRepository = frequenciesRepository;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
            this.logger = logger;
        }

        public async Task Import(FrequencySetFile file, bool convertToPGroups)
        {
            if (file.Contents == null)
            {
                throw new EmptyHaplotypeFileException();
            }

            // Load all frequencies into memory, to perform aggregation by PGroup.
            // Largest known HF set is ~300,000 entries, which is reasonable to load into memory here.
            var haplotypeFrequencyFile = frequencyCsvReader.GetFrequencies(file.Contents).ToList();

            var frequencySetData =  new HaplotypeFrequencySetMetadata(haplotypeFrequencyFile.First(), file.FileName);

            var set = await AddNewInactiveSet(frequencySetData);

            var haplotypeFrequency = haplotypeFrequencyFile.Select(f => new HaplotypeFrequency
            {
                A = f.A,
                B = f.B,
                C = f.C,
                DQB1 = f.Dqb1,
                DRB1 = f.Drb1,
                Frequency = f.Frequency,
                TypingCategory = HaplotypeTypingCategory.GGroup
            }).ToList();

            await StoreFrequencies(haplotypeFrequency, set.Id, convertToPGroups, frequencySetData.HlaNomenclatureVersion);
            await setRepository.ActivateSet(set.Id);
        }

        private async Task<HaplotypeFrequencySet> AddNewInactiveSet(HaplotypeFrequencySetMetadata metadata)
        {
            if (!metadata.Ethnicity.IsNullOrEmpty() && metadata.Registry.IsNullOrEmpty())
            {
                throw new MalformedHaplotypeFileException($"Cannot import set: Ethnicity ('{metadata.Ethnicity}') provided but no registry");
            }

            var newSet = new HaplotypeFrequencySet
            {
                RegistryCode = metadata.Registry,
                EthnicityCode = metadata.Ethnicity,
                HlaNomenclatureVersion = metadata.HlaNomenclatureVersion,
                PopulationId = metadata.PopulationId,
                Active = false,
                Name = metadata.Name,
                DateTimeAdded = DateTimeOffset.Now
            };

            return await setRepository.AddSet(newSet);
        }

        private async Task StoreFrequencies(
            IReadOnlyCollection<HaplotypeFrequency> gGroupHaplotypes,
            int setId,
            bool convertToPGroups,
            string hlaNomenclatureVersion)
        {
            var haplotypes = gGroupHaplotypes.Select(r => r.Hla).ToList();

            if (haplotypes.Count != haplotypes.Distinct().Count())
            {
                throw new DuplicateHaplotypeImportException();
            }

            if (!gGroupHaplotypes.Any())
            {
                throw new EmptyHaplotypeFileException();
            }

            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);

            var haplotypesToStore = convertToPGroups
                ? await ConvertHaplotypesToPGroupResolutionAndConsolidate(gGroupHaplotypes, hlaMetadataDictionary)
                : gGroupHaplotypes;

            if (!convertToPGroups)
            {
                var areAllGGroupsValid = await ValidateHaplotypes(gGroupHaplotypes, hlaMetadataDictionary);

                if (!areAllGGroupsValid)
                {
                    throw new MalformedHaplotypeFileException("Invalid Hla.");
                }
            }

            await frequenciesRepository.AddHaplotypeFrequencies(setId, haplotypesToStore);
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

        private static async Task<bool> ValidateHaplotypes(IEnumerable<HaplotypeFrequency> frequencies, IHlaMetadataDictionary hlaMetadataDictionary)
        {
            var haplotypes = frequencies.Select(hf => hf.Hla).ToList();

            var gGroupsPerLocus = new LociInfo<int>().Map((locus, _) => haplotypes.Select(h => h.GetLocus(locus)).ToHashSet());

            var validationResults = await gGroupsPerLocus.MapAsync(async (locus, gGroups) =>
            {
                return await Task.WhenAll(gGroups.Select(gGroup => ValidateGGroup(locus, gGroup, hlaMetadataDictionary)));
            });

            return validationResults.AllAtLoci(results => results.All(x => x));
        }

        private static async Task<bool> ValidateGGroup(Locus locus, string gGroup, IHlaMetadataDictionary hlaMetadataDictionary)
        {
            return !LocusSettings.MatchPredictionLoci.Contains(locus)
                   || await hlaMetadataDictionary.ValidateHla(locus, gGroup, TargetHlaCategory.GGroup);
        }
    }
}