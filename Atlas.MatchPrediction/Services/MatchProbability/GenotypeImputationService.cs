using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;
using PhenotypeOfStrings = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;
using GenotypeOfKnownTypingCategory = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<Atlas.MatchPrediction.ExternalInterface.Models.HlaAtKnownTypingCategory>;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public class ImputationInput
    {
        public SubjectData SubjectData { get; set; }
        public MatchPredictionParameters MatchPredictionParameters { get; set; }
    }

    public interface IGenotypeImputationService
    {
        /// <summary>
        /// Expands <see cref="SubjectData.HlaTyping"/> to set of possible genotypes with their likelihoods.
        /// </summary>
        /// <returns>A set of the most likely genotypes, truncated from the full set of possible genotypes.
        /// <see cref="ExpandedGenotypeTruncater"/> for more info on set truncation.</returns>
        Task<ImputedGenotypes> Impute(ImputationInput input);
    }

    internal class GenotypeImputationService : IGenotypeImputationService
    {
        private const string LoggingPrefix = "MatchPrediction: ";
        private readonly ICompressedPhenotypeExpander compressedPhenotypeExpander;
        private readonly IGenotypeLikelihoodService genotypeLikelihoodService;
        private readonly ILogger logger;

        public GenotypeImputationService(
            ICompressedPhenotypeExpander compressedPhenotypeExpander,
            IGenotypeLikelihoodService genotypeLikelihoodService,
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger)
        {
            this.compressedPhenotypeExpander = compressedPhenotypeExpander;
            this.genotypeLikelihoodService = genotypeLikelihoodService;
            this.logger = logger;
        }

        /// <inheritdoc />
        public async Task<ImputedGenotypes> Impute(ImputationInput input)
        {
            var genotypes = await ExpandToGenotypes(input);

            if (genotypes.IsNullOrEmpty())
            {
                logger.SendTrace($"{LoggingPrefix}{input.SubjectData.SubjectFrequencySet.SubjectLogDescription} genotype unrepresented.", LogLevel.Verbose);
                return ImputedGenotypes.Empty();
            }

            var genotypeLikelihoods = await CalculateGenotypeLikelihoods(
                genotypes, input.SubjectData.SubjectFrequencySet.FrequencySet, input.MatchPredictionParameters.AllowedLoci);

            return ExpandedGenotypeTruncater.TruncateGenotypes(genotypeLikelihoods, genotypes);
        }

        private async Task<ISet<GenotypeOfKnownTypingCategory>> ExpandToGenotypes(ImputationInput input)
        {
            using (logger.RunTimed($"{LoggingPrefix}Expand {input.SubjectData.SubjectFrequencySet.SubjectLogDescription} phenotype", LogLevel.Verbose))
            {
                var frequencySet = input.SubjectData.SubjectFrequencySet.FrequencySet;

                var expanderInput = new CompressedPhenotypeExpanderInput
                {
                    Phenotype = input.SubjectData.HlaTyping,
                    HfSetId = frequencySet.Id,
                    HfSetHlaNomenclatureVersion = frequencySet.HlaNomenclatureVersion,
                    MatchPredictionParameters = input.MatchPredictionParameters
                };

                return await compressedPhenotypeExpander.ExpandCompressedPhenotype(expanderInput);
            }
        }

        private async Task<Dictionary<PhenotypeOfStrings, decimal>> CalculateGenotypeLikelihoods(
            IEnumerable<GenotypeOfKnownTypingCategory> genotypes,
            HaplotypeFrequencySet frequencySet,
            ISet<Locus> allowedLoci)
        {
            using (logger.RunTimed($"{LoggingPrefix}Calculate likelihoods for genotypes", LogLevel.Verbose))
            {
                var genotypeLikelihoods = new List<KeyValuePair<PhenotypeOfStrings, decimal>>();

                var stringGenotypes = genotypes.Select(g => g.ToHlaNames()).ToHashSet();

                // If there is no ambiguity for an input genotype, we do not need to use haplotype frequencies to work out the likelihood of said genotype - it is already guaranteed! 
                if (stringGenotypes.Count == 1)
                {
                    genotypeLikelihoods.Add(new KeyValuePair<PhenotypeOfStrings, decimal>(stringGenotypes.Single(), 1));
                }
                else
                {
                    foreach (var genotype in stringGenotypes)
                    {
                        genotypeLikelihoods.Add(await CalculateLikelihood(genotype, frequencySet, allowedLoci));
                    }
                }

                return genotypeLikelihoods.ToDictionary();
            }
        }

        private async Task<KeyValuePair<PhenotypeOfStrings, decimal>> CalculateLikelihood(
            PhenotypeOfStrings diplotype,
            HaplotypeFrequencySet frequencySet,
            ISet<Locus> allowedLoci)
        {
            var likelihood = await genotypeLikelihoodService.CalculateLikelihoodForDiplotype(diplotype, frequencySet, allowedLoci);
            return new KeyValuePair<PhenotypeOfStrings, decimal>(diplotype, likelihood);
        }
    }
}