using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public interface IMatchProbabilityService
    {
        public Task<MatchProbabilityResponse> CalculateMatchProbability(MatchProbabilityInput matchProbabilityInput);
    }

    internal class MatchProbabilityService : IMatchProbabilityService
    {
        private const string LoggingPrefix = "MatchPrediction: ";

        private readonly ICompressedPhenotypeExpander compressedPhenotypeExpander;
        private readonly IGenotypeLikelihoodService genotypeLikelihoodService;
        private readonly IGenotypeMatcher genotypeMatcher;
        private readonly IMatchProbabilityCalculator matchProbabilityCalculator;
        private readonly ILogger logger;

        public MatchProbabilityService(
            ICompressedPhenotypeExpander compressedPhenotypeExpander,
            IGenotypeLikelihoodService genotypeLikelihoodService,
            IGenotypeMatcher genotypeMatcher,
            IMatchProbabilityCalculator matchProbabilityCalculator,
            ILogger logger)
        {
            this.compressedPhenotypeExpander = compressedPhenotypeExpander;
            this.genotypeLikelihoodService = genotypeLikelihoodService;
            this.genotypeMatcher = genotypeMatcher;
            this.matchProbabilityCalculator = matchProbabilityCalculator;
            this.logger = logger;
        }

        public async Task<MatchProbabilityResponse> CalculateMatchProbability(MatchProbabilityInput matchProbabilityInput)
        {
            var patientGenotypes = await logger.RunTimedAsync(
                async () => await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                    matchProbabilityInput.PatientHla,
                    matchProbabilityInput.HlaNomenclatureVersion
                ),
                $"{LoggingPrefix}Expanded patient phenotype",
                LogLevel.Verbose
            );

            var donorGenotypes = await logger.RunTimedAsync(
                async () => await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                    matchProbabilityInput.DonorHla,
                    matchProbabilityInput.HlaNomenclatureVersion
                ),
                $"{LoggingPrefix}Expanded donor phenotype",
                LogLevel.Verbose
            );

            var matchingPairs = await logger.RunTimedAsync(
                async () => await genotypeMatcher.PairsWithTenOutOfTenMatch(
                    patientGenotypes,
                    donorGenotypes, matchProbabilityInput.HlaNomenclatureVersion
                ),
                $"{LoggingPrefix}Calculated genotype matches",
                LogLevel.Verbose
            );

            // Returns early when patient/donor pair are guaranteed to either be a match or not
            if (patientGenotypes.Count * donorGenotypes.Count == matchingPairs.Count)
            {
                return new MatchProbabilityResponse {ZeroMismatchProbability = 1m};
            }

            if (matchingPairs.Count == 0)
            {
                return new MatchProbabilityResponse {ZeroMismatchProbability = 0m};
            }

            var genotypes = patientGenotypes.Union(donorGenotypes);


            var genotypesLikelihoods = (await logger.RunTimedAsync(
                    async () => await Task.WhenAll(genotypes.Select(CalculateLikelihood)),
                    $"{LoggingPrefix}Calculated likelihoods for genotypes",
                    LogLevel.Verbose
                ))
                .ToDictionary();

            var probability =
                matchProbabilityCalculator.CalculateMatchProbability(patientGenotypes, donorGenotypes, matchingPairs, genotypesLikelihoods);

            return new MatchProbabilityResponse {ZeroMismatchProbability = probability};
        }

        private async Task<KeyValuePair<PhenotypeInfo<string>, decimal>> CalculateLikelihood(PhenotypeInfo<string> genotype)
        {
            var likelihood = await genotypeLikelihoodService.CalculateLikelihood(genotype);
            return new KeyValuePair<PhenotypeInfo<string>, decimal>(genotype, likelihood);
        }
    }
}