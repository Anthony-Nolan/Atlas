using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using PhenotypeOfStrings = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;
using Atlas.MatchPrediction.ApplicationInsights;

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
        private readonly IAtlasLogger logger;
        private readonly GenotypeImputationSettings settings;

        // IDiplotypeLikelihoodCalculator is deliberately gone from this constructor rather than kept and unused:
        // ATL-233 T2 moved likelihood calculation into the expansion, where each genotype's haplotype pair is still
        // in hand. The service remains registered for its other (public interface) consumers.
        public GenotypeImputationService(
            ICompressedPhenotypeExpander compressedPhenotypeExpander,
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
            GenotypeImputationSettings settings)
        {
            this.compressedPhenotypeExpander = compressedPhenotypeExpander;
            this.logger = logger;
            this.settings = settings;
        }

        /// <inheritdoc />
        public async Task<ImputedGenotypes> Impute(ImputationInput input)
        {
            var expanded = await ExpandToGenotypes(input);
            var genotypes = expanded.Genotypes;

            if (genotypes.IsNullOrEmpty())
            {
                logger.SendTrace($"{LoggingPrefix}{input.SubjectData.SubjectFrequencySet.SubjectLogDescription} genotype unrepresented.", LogLevel.Verbose);
                return ImputedGenotypes.Empty();
            }

            logger.SendTrace($"Filtered expanded genotypes: {genotypes.Count}");

            // ATL-233 T2: the likelihoods were computed where the pair that produced each genotype was still in hand,
            // at one frequency resolution per survivor instead of two awaited lookups per genotype. What is left here
            // is the certainty rule, which never needed a frequency at all.
            var genotypeLikelihoods = expanded.Likelihoods;

            // If there is no ambiguity for an input genotype, we do not need to use haplotype frequencies to work out the likelihood of said genotype - it is already guaranteed!
            if (genotypeLikelihoods.Count == 1)
            {
                genotypeLikelihoods = new Dictionary<PhenotypeOfStrings, decimal> { [genotypeLikelihoods.Keys.Single()] = 1 };
            }

            return ExpandedGenotypeTruncater.TruncateGenotypes(genotypeLikelihoods, genotypes, settings.MaximumExpandedGenotypesPerInput);
        }

        private async Task<ExpandedGenotypes> ExpandToGenotypes(ImputationInput input)
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

    }
}