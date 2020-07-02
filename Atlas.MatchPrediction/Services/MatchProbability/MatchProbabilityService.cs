using System;
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
            var patientGenotypes = await ExpandPatientPhenotype(matchProbabilityInput);
            var donorGenotypes = await ExpandDonorPhenotype(matchProbabilityInput);

            var matchingPairs = await CalculateMatchingPairs(matchProbabilityInput, patientGenotypes, donorGenotypes);

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


            var genotypesLikelihoods = await CalculateGenotypeLikelihoods(genotypes);

            var probability =
                matchProbabilityCalculator.CalculateMatchProbability(patientGenotypes, donorGenotypes, matchingPairs, genotypesLikelihoods);

            return new MatchProbabilityResponse {ZeroMismatchProbability = probability};
        }

        private async Task<ISet<PhenotypeInfo<string>>> ExpandPatientPhenotype(MatchProbabilityInput matchProbabilityInput)
        {
            return await logger.RunTimedAsync(
                async () => await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                    matchProbabilityInput.PatientHla,
                    matchProbabilityInput.HlaNomenclatureVersion
                ),
                $"{LoggingPrefix}Expanded patient phenotype",
                LogLevel.Verbose
            );
        }

        private async Task<ISet<PhenotypeInfo<string>>> ExpandDonorPhenotype(MatchProbabilityInput matchProbabilityInput)
        {
            return await logger.RunTimedAsync(
                async () => await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                    matchProbabilityInput.DonorHla,
                    matchProbabilityInput.HlaNomenclatureVersion
                ),
                $"{LoggingPrefix}Expanded donor phenotype",
                LogLevel.Verbose
            );
        }

        private async Task<ISet<Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>>> CalculateMatchingPairs(
            MatchProbabilityInput matchProbabilityInput,
            ISet<PhenotypeInfo<string>> patientGenotypes,
            ISet<PhenotypeInfo<string>> donorGenotypes)
        {
            return await logger.RunTimedAsync(
                async () => await genotypeMatcher.PairsWithTenOutOfTenMatch(
                    patientGenotypes,
                    donorGenotypes, matchProbabilityInput.HlaNomenclatureVersion
                ),
                $"{LoggingPrefix}Calculated genotype matches",
                LogLevel.Verbose
            );
        }

        private async Task<Dictionary<PhenotypeInfo<string>, decimal>> CalculateGenotypeLikelihoods(IEnumerable<PhenotypeInfo<string>> genotypes)
        {
            return (await logger.RunTimedAsync(
                    async () => await Task.WhenAll(genotypes.Select(CalculateLikelihood)),
                    $"{LoggingPrefix}Calculated likelihoods for genotypes",
                    LogLevel.Verbose
                ))
                .ToDictionary();
        }

        private async Task<KeyValuePair<PhenotypeInfo<string>, decimal>> CalculateLikelihood(PhenotypeInfo<string> genotype)
        {
            var likelihood = await genotypeLikelihoodService.CalculateLikelihood(genotype);
            return new KeyValuePair<PhenotypeInfo<string>, decimal>(genotype, likelihood);
        }
    }
}