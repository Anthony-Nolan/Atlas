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
        private readonly IFrequencyJsonReader frequencyJsonReader;
        private readonly IHaplotypeFrequencySetRepository setRepository;
        private readonly IHaplotypeFrequenciesRepository frequenciesRepository;
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private readonly ILogger logger;

        public FrequencySetImporter(
            IFrequencyJsonReader frequencyJsonReader,
            IHaplotypeFrequencySetRepository setRepository,
            IHaplotypeFrequenciesRepository frequenciesRepository,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            ILogger logger)
        {
            this.frequencyJsonReader = frequencyJsonReader;
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
            var haplotypeFrequencyFile = frequencyJsonReader.GetFrequencies(file.Contents);

            var setIds = (await AddNewInactiveSet(haplotypeFrequencyFile, file.FileName)).Select(hf => hf.Id).ToList();

            var haplotypeFrequency = haplotypeFrequencyFile.Frequencies.Select(f => new HaplotypeFrequency
            {
                A = f.A,
                B = f.B,
                C = f.C,
                DQB1 = f.Dqb1,
                DRB1 = f.Drb1,
                Frequency = f.Frequency,
                TypingCategory = HaplotypeTypingCategory.GGroup
            }).ToList();

            await StoreFrequencies(haplotypeFrequency, haplotypeFrequencyFile.NomenclatureVersion, setIds, convertToPGroups);

            foreach (var setId in setIds)
            {
                await setRepository.ActivateSet(setId);
            }
        }

        private async Task<IEnumerable<HaplotypeFrequencySet>> AddNewInactiveSet(FrequencySetFileSchema metadata, string fileName)
        {
            return await TaskExtensions.WhenEach(metadata.RegistryCodes.Select(registry =>
            {
                var newSet = new HaplotypeFrequencySet
                {
                    RegistryCode = registry,
                    EthnicityCode = metadata.Ethnicity,
                    HlaNomenclatureVersion = metadata.NomenclatureVersion,
                    PopulationId = metadata.PopulationId,
                    Active = false,
                    Name = fileName,
                    DateTimeAdded = DateTimeOffset.Now
                };

                return setRepository.AddSet(newSet);
            }));
        }

        private async Task StoreFrequencies(
            IReadOnlyCollection<HaplotypeFrequency> gGroupHaplotypes,
            string hlaNomenclatureVersion,
            IEnumerable<int> setIds,
            bool convertToPGroups)
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

            var haplotypesToStore = (convertToPGroups
                ? await ConvertHaplotypesToPGroupResolutionAndConsolidate(gGroupHaplotypes, hlaMetadataDictionary)
                : gGroupHaplotypes).ToList();

            if (!convertToPGroups)
            {
                var areAllGGroupsValid = await ValidateHaplotypes(gGroupHaplotypes, hlaMetadataDictionary);

                if (!areAllGGroupsValid)
                {
                    throw new MalformedHaplotypeFileException("Invalid Hla.");
                }
            }

            foreach (var setId in setIds)
            {
                await frequenciesRepository.AddHaplotypeFrequencies(setId, haplotypesToStore);
            }
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