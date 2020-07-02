using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.MatchCalculation;

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
        private readonly IMatchCalculationService matchCalculationService;
        private readonly IMatchProbabilityCalculator matchProbabilityCalculator;
        private readonly ILogger logger;

        public MatchProbabilityService(
            ICompressedPhenotypeExpander compressedPhenotypeExpander,
            IGenotypeLikelihoodService genotypeLikelihoodService,
            IMatchCalculationService matchCalculationService,
            IMatchProbabilityCalculator matchProbabilityCalculator,
            ILogger logger)
        {
            this.compressedPhenotypeExpander = compressedPhenotypeExpander;
            this.genotypeLikelihoodService = genotypeLikelihoodService;
            this.matchCalculationService = matchCalculationService;
            this.matchProbabilityCalculator = matchProbabilityCalculator;
            this.logger = logger;
        }

        public async Task<MatchProbabilityResponse> CalculateMatchProbability(MatchProbabilityInput matchProbabilityInput)
        {
            var patientGenotypes = await ExpandPatientPhenotype(matchProbabilityInput);
            var donorGenotypes = await ExpandDonorPhenotype(matchProbabilityInput);

            var matchingPairs = await CalculateMatchingPairs(matchProbabilityInput, patientGenotypes, donorGenotypes);

            var allPatientDonorCombinations = patientGenotypes.SelectMany(patientHla =>
                donorGenotypes.Select(donorHla => new Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>(patientHla, donorHla)));

            var patientDonorMatchDetails =
                (await Task.WhenAll(allPatientDonorCombinations
                    .Select(pd => CalculateMatch(pd, matchProbabilityInput.HlaNomenclatureVersion)))).ToHashSet();

            // Returns early when patient/donor pair are guaranteed to either be a match or not
            if (patientDonorMatchDetails.Any() && patientDonorMatchDetails.All(p => p.IsTenOutOfTenMatch))
            {
                return new MatchProbabilityResponse {ZeroMismatchProbability = 1m};
            }
            if (!patientDonorMatchDetails.Any(p => p.IsTenOutOfTenMatch))
            {
                return new MatchProbabilityResponse {ZeroMismatchProbability = 0m};
            }

            var genotypes = patientGenotypes.Union(donorGenotypes);

            var genotypesLikelihoods = await CalculateGenotypeLikelihoods(genotypes);
            return matchProbabilityCalculator.CalculateMatchProbability(patientGenotypes, donorGenotypes, patientDonorMatchDetails, genotypesLikelihoods);
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

        private async Task<GenotypeMatchDetails> CalculateMatch(
            Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>> patientDonorPair,
            string hlaNomenclatureVersion)
        {
            return await matchCalculationService.MatchAtPGroupLevel(patientDonorPair.Item1, patientDonorPair.Item2, hlaNomenclatureVersion);
        }

        private async Task<KeyValuePair<PhenotypeInfo<string>, decimal>> CalculateLikelihood(PhenotypeInfo<string> genotype)
        {
            var likelihood = await genotypeLikelihoodService.CalculateLikelihood(genotype);
            return new KeyValuePair<PhenotypeInfo<string>, decimal>(genotype, likelihood);
        }
    }
}