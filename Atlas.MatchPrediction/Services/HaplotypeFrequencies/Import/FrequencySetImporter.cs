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
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Models.FileSchema;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions;
using Atlas.MatchPrediction.Utils;
using TaskExtensions = Atlas.Common.Utils.Tasks.TaskExtensions;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import
{
    public class FrequencySetImportBehaviour
    {
        /// <summary>
        /// When set, the import process will convert haplotypes to PGroup typing where possible (i.e. when haplotype has no null expressing GGroups).
        /// For any haplotypes that are different at G-Group level, but the same at P-Group, frequency values will be consolidated.
        /// 
        /// Defaults to true, as this yields a significantly faster algorithm.
        /// 
        /// When set to false, all frequencies will be imported at the original G-Group resolutions.
        /// This is only expected to be used in test code, where it is much easier to keep track of a single set of frequencies,
        /// than of GGroup typed haplotypes *and* their corresponding P-Group typed ones.
        /// </summary>
        public bool ShouldConvertLargeGGroupsToPGroups { get; set; } = true;

        /// <summary>
        /// Allows conditional bypass of validation of input frequency values. Useful for testing, as this is the slowest part of the import process.
        /// </summary>
        public bool ShouldBypassHlaValidation { get; set; } = false;
    }
    
    internal interface IFrequencySetImporter
    {
        Task Import(FrequencySetFile file, FrequencySetImportBehaviour importBehaviour);
    }

    internal class FrequencySetImporter : IFrequencySetImporter
    {
        private readonly IFrequencyFileParser frequencyFileParser;
        private readonly IHaplotypeFrequencySetRepository setRepository;
        private readonly IHaplotypeFrequenciesRepository frequenciesRepository;
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private readonly IFrequencySetValidator frequencySetValidator;
        private readonly ILogger logger;

        public FrequencySetImporter(
            IFrequencyFileParser frequencyFileParser,
            IHaplotypeFrequencySetRepository setRepository,
            IHaplotypeFrequenciesRepository frequenciesRepository,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            IFrequencySetValidator frequencySetValidator,
            ILogger logger)
        {
            this.frequencyFileParser = frequencyFileParser;
            this.setRepository = setRepository;
            this.frequenciesRepository = frequenciesRepository;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
            this.frequencySetValidator = frequencySetValidator;
            this.logger = logger;
        }

        public async Task Import(FrequencySetFile file, FrequencySetImportBehaviour importBehaviour)
        {
            if (file.Contents == null)
            {
                throw new EmptyHaplotypeFileException();
            }

            // Load all frequencies into memory, to perform aggregation by PGroup.
            // Largest known HF set is ~300,000 entries, which is reasonable to load into memory here.
            var frequencySet = frequencyFileParser.GetFrequencies(file.Contents);

            frequencySetValidator.ValidateNonHlaDataAndThrow(frequencySet);

            var setIds = await AddNewInactiveSets(frequencySet, file.FileName);
            
            // ReSharper disable once PossibleInvalidOperationException - non null, enforced by Validator
            var frequencySetTypingCategory = frequencySet.TypingCategory.Value;
            
            var inputHaplotypes = frequencySet.Frequencies.Select(f => new HaplotypeFrequency
            {
                A = f.A,
                B = f.B,
                C = f.C,
                DQB1 = f.Dqb1,
                DRB1 = f.Drb1,
                Frequency = f.Frequency,
                TypingCategory = frequencySetTypingCategory.ToDatabaseTypingCategory()
            }).ToList();

            await StoreFrequencies(
                inputHaplotypes,
                frequencySet.HlaNomenclatureVersion,
                setIds,
                frequencySetTypingCategory,
                importBehaviour);

            foreach (var setId in setIds)
            {
                try
                {
                    await setRepository.ActivateSet(setId);
                }
                catch
                {
                    await frequenciesRepository.RemoveHaplotypeFrequencies(setId);
                    throw;
                }
            }
        }

        private async Task<List<int>> AddNewInactiveSets(FrequencySetFileSchema metadata, string fileName)
        {
            var ethnicityCodes = metadata.EthnicityCodes.IsNullOrEmpty() ? new string[] {null} : metadata.EthnicityCodes;
            var registryCodes = metadata.RegistryCodes.IsNullOrEmpty() ? new string[] {null} : metadata.RegistryCodes;

            var setIds = new List<int>();

            foreach (var registry in registryCodes)
            {
                foreach (var ethnicity in ethnicityCodes)
                {
                    var newSet = new HaplotypeFrequencySet
                    {
                        RegistryCode = registry,
                        EthnicityCode = ethnicity,
                        HlaNomenclatureVersion = metadata.HlaNomenclatureVersion,
                        PopulationId = metadata.PopulationId,
                        Active = false,
                        Name = fileName,
                        DateTimeAdded = DateTimeOffset.Now
                    };

                    var set = await setRepository.AddSet(newSet);
                    setIds.Add(set.Id);
                }
            }

            return setIds;
        }

        private async Task StoreFrequencies(
            IReadOnlyCollection<HaplotypeFrequency> inputHaplotypes,
            string hlaNomenclatureVersion,
            IEnumerable<int> setIds,
            ImportTypingCategory typingCategory,
            FrequencySetImportBehaviour importBehaviour)
        {
            var haplotypes = inputHaplotypes.Select(r => r.Hla).ToList();

            if (haplotypes.Count != haplotypes.Distinct().Count())
            {
                throw new DuplicateHaplotypeImportException();
            }

            if (!inputHaplotypes.Any())
            {
                throw new EmptyHaplotypeFileException();
            }

            if (!importBehaviour.ShouldBypassHlaValidation)
            {
                await frequencySetValidator.ValidateHlaDataAndThrow(inputHaplotypes, hlaNomenclatureVersion, typingCategory);
            }

            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);
            var haplotypesToStore = (importBehaviour.ShouldConvertLargeGGroupsToPGroups && typingCategory == ImportTypingCategory.LargeGGroup
                ? await ConvertHaplotypesToPGroupResolutionAndConsolidate(inputHaplotypes, hlaMetadataDictionary)
                : inputHaplotypes).ToList();

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
    }
}