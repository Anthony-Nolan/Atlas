using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Models.FileSchema;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import.Exceptions;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import
{
    internal interface IFrequencySetValidator
    {
        /// <summary>
        /// Validates all data in the HF set, excluding HLA values - these require dynamic validation against an up to date nomenclature source,
        /// which is covered in <see cref="ValidateHlaDataAndThrow"/>
        /// </summary>
        void ValidateNonHlaDataAndThrow(FrequencySetFileSchema frequencySetFile);

        Task ValidateHlaDataAndThrow(
            IEnumerable<HaplotypeFrequency> haplotypeFrequencies,
            string hlaNomenclatureVersion,
            ImportTypingCategory typingCategory);
    }

    internal class FrequencySetValidator : IFrequencySetValidator
    {
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private readonly ILogger logger;

        public FrequencySetValidator(IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory, ILogger logger)
        {
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
            this.logger = logger;
        }

        public void ValidateNonHlaDataAndThrow(FrequencySetFileSchema frequencySetFile)
        {
            if (frequencySetFile.TypingCategory == null)
            {
                throw new MalformedHaplotypeFileException("Cannot import set: Typing Category must be specified");
            }

            if (!frequencySetFile.EthnicityCodes.IsNullOrEmpty())
            {
                if (frequencySetFile.RegistryCodes.IsNullOrEmpty())
                {
                    throw new MalformedHaplotypeFileException(
                        $"Cannot import set: Ethnicity codes ([{frequencySetFile.EthnicityCodes.StringJoin(", ")}]) provided but no registry");
                }

                if (frequencySetFile.EthnicityCodes.Length != frequencySetFile.EthnicityCodes.ToHashSet().Count)
                {
                    throw new MalformedHaplotypeFileException($"Cannot import set: Cannot import duplicate registry codes");
                }
            }

            if (!frequencySetFile.RegistryCodes.IsNullOrEmpty())
            {
                if (frequencySetFile.RegistryCodes.Contains(null))
                {
                    throw new MalformedHaplotypeFileException("Cannot import set: Invalid registry codes");
                }

                if (frequencySetFile.RegistryCodes.Length != frequencySetFile.RegistryCodes.ToHashSet().Count)
                {
                    throw new MalformedHaplotypeFileException($"Cannot import set: Cannot import duplicate registry codes");
                }
            }

            if (frequencySetFile.HlaNomenclatureVersion.IsNullOrEmpty())
            {
                throw new MalformedHaplotypeFileException("Cannot import set: Nomenclature version must be set");
            }

            foreach (var frequencyRecord in frequencySetFile.Frequencies)
            {
                if (frequencyRecord == null)
                {
                    throw new MalformedHaplotypeFileException("Set does not contain any frequencies");
                }

                if (frequencyRecord.Frequency == 0m)
                {
                    throw new MalformedHaplotypeFileException($"Haplotype frequency property frequency cannot be 0.");
                }

                if (frequencyRecord.A == null ||
                    frequencyRecord.B == null ||
                    frequencyRecord.C == null ||
                    frequencyRecord.Dqb1 == null ||
                    frequencyRecord.Drb1 == null)
                {
                    throw new MalformedHaplotypeFileException($"Haplotype frequency loci cannot be null.");
                }
            }
        }

        /// <inheritdoc />
        public async Task ValidateHlaDataAndThrow(
            IEnumerable<HaplotypeFrequency> haplotypeFrequencies,
            string hlaNomenclatureVersion,
            ImportTypingCategory typingCategory)
        {
            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);

            var targetValidationCategory = typingCategory.ToHlaValidationCategory();

            var haplotypes = haplotypeFrequencies.Select(hf => hf.Hla).ToList();

            var hlaNamesPerLocus = new LociInfo<int>().Map((locus, _) => haplotypes.Select(h => h.GetLocus(locus)).ToHashSet());

            async Task<bool> ValidateHlaAtLocus(Locus locus, string hla)
            {
                return !LocusSettings.MatchPredictionLoci.Contains(locus) ||
                       await hlaMetadataDictionary.ValidateHla(locus, hla, targetValidationCategory);
            }

            var validationResults = await hlaNamesPerLocus.MapAsync(async (locus, hlaSet) =>
                await Task.WhenAll(hlaSet.Select(async hlaName => (hlaName, await ValidateHlaAtLocus(locus, hlaName)))));


            var invalidHla = validationResults.Map(resultsAtLocus => 
                resultsAtLocus
                    .Where(validationResult => !validationResult.Item2)
                    .Select(validationResult => validationResult.hlaName)
                    .ToList()
                );
            
            if (invalidHla.AnyAtLoci(results => results.Any()))
            {
                invalidHla.ForEachLocus((l, invalidHlaAtLocus) =>
                {
                    if (invalidHlaAtLocus.Any())
                    {
                        logger.SendTrace($"Invalid HLA at locus {l}: {invalidHlaAtLocus.StringJoin(",")}");
                    }
                });
                
                throw new MalformedHaplotypeFileException(
                    $"Invalid Hla. Expected all provided frequencies to be valid hla of typing resolution: {typingCategory}. See AI logs for specific failed values.");
            }
            
        }
    }
}