using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
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
        private readonly IFrequencySetService frequencySetService;
        private readonly ILogger logger;

        public MatchProbabilityService(
            ICompressedPhenotypeExpander compressedPhenotypeExpander,
            IGenotypeLikelihoodService genotypeLikelihoodService,
            IMatchCalculationService matchCalculationService,
            IMatchProbabilityCalculator matchProbabilityCalculator,
            IFrequencySetService frequencySetService,
            ILogger logger)
        {
            this.compressedPhenotypeExpander = compressedPhenotypeExpander;
            this.genotypeLikelihoodService = genotypeLikelihoodService;
            this.matchCalculationService = matchCalculationService;
            this.matchProbabilityCalculator = matchProbabilityCalculator;
            this.frequencySetService = frequencySetService;
            this.logger = logger;
        }

        public async Task<MatchProbabilityResponse> CalculateMatchProbability(
            MatchProbabilityInput matchProbabilityInput)
        {

            var patientGenotypes = await ExpandPatientPhenotype(matchProbabilityInput);
            var donorGenotypes = await ExpandDonorPhenotype(matchProbabilityInput);

            var allPatientDonorCombinations = patientGenotypes.SelectMany(patientHla =>
                donorGenotypes.Select(donorHla =>
                    new Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>(patientHla, donorHla)));

            var patientDonorMatchDetails =
                await CalculatePairsMatchCounts(matchProbabilityInput, allPatientDonorCombinations);

            // Returns early when patient/donor pair are guaranteed to either be a match or not
            if (patientDonorMatchDetails.Any() && patientDonorMatchDetails.All(p => p.MatchCount == 10))
            {
                return new MatchProbabilityResponse
                {
                    ZeroMismatchProbability = 1m,
                    OneMismatchProbability = 0m,
                    TwoMismatchProbability = 0m,
                    ZeroMismatchProbabilityPerLocus = new LociInfo<decimal?>
                        {A = 1m, B = 1m, C = 1m, Dpb1 = null, Dqb1 = 1m, Drb1 = 1m}
                };
            }

            var frequencySets = await frequencySetService.GetHaplotypeFrequencySets(
                matchProbabilityInput.DonorFrequencySetMetadata, matchProbabilityInput.PatientFrequencySetMetadata);
            
            var patientGenotypeLikelihoods = await CalculateGenotypeLikelihoods(patientGenotypes, frequencySets.PatientSet);
            var donorGenotypeLikelihoods = await CalculateGenotypeLikelihoods(donorGenotypes, frequencySets.DonorSet);

            var genotypesLikelihoods = patientGenotypeLikelihoods.Union(donorGenotypeLikelihoods).ToDictionary();
            
            return matchProbabilityCalculator.CalculateMatchProbability(patientGenotypes, donorGenotypes,
                patientDonorMatchDetails, genotypesLikelihoods);
        }

        private async Task<ISet<PhenotypeInfo<string>>> ExpandPatientPhenotype(
            MatchProbabilityInput matchProbabilityInput)
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

        private async Task<ISet<PhenotypeInfo<string>>> ExpandDonorPhenotype(
            MatchProbabilityInput matchProbabilityInput)
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

        private async Task<ISet<GenotypeMatchDetails>> CalculatePairsMatchCounts(
            MatchProbabilityInput matchProbabilityInput,
            IEnumerable<Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>> allPatientDonorCombinations)
        {
            return await logger.RunTimedAsync(
                async () => (await Task.WhenAll(allPatientDonorCombinations
                    .Select(pd => CalculateMatch(pd, matchProbabilityInput.HlaNomenclatureVersion)))).ToHashSet(),
                $"{LoggingPrefix}Calculated genotype matches",
                LogLevel.Verbose
            );
        }

        private async Task<Dictionary<PhenotypeInfo<string>, decimal>> CalculateGenotypeLikelihoods(
            ISet<PhenotypeInfo<string>> genotypes,
            HaplotypeFrequencySet frequencySet)
        {
            return (await logger.RunTimedAsync(
                    async () => await Task.WhenAll(genotypes.Select(genotype => CalculateLikelihood(genotype, frequencySet))),
                    $"{LoggingPrefix}Calculated likelihoods for genotypes",
                    LogLevel.Verbose
                ))
                .ToDictionary();
        }

        private async Task<GenotypeMatchDetails> CalculateMatch(
            Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>> patientDonorPair,
            string hlaNomenclatureVersion)
        {
            return await matchCalculationService.MatchAtPGroupLevel(patientDonorPair.Item1, patientDonorPair.Item2,
                hlaNomenclatureVersion);
        }

        private async Task<KeyValuePair<PhenotypeInfo<string>, decimal>> CalculateLikelihood(
            PhenotypeInfo<string> phenotype, 
            HaplotypeFrequencySet frequencySet)
        {
            var likelihood = await genotypeLikelihoodService.CalculateLikelihood(phenotype, frequencySet);
            return new KeyValuePair<PhenotypeInfo<string>, decimal>(phenotype, likelihood);
        }
    }
}