using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Client.Models.MatchProbability;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.Common.Utils.Extensions;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public interface IMatchProbabilityService
    {
        public Task<MatchProbabilityResponse> CalculateMatchProbability(MatchProbabilityInput matchProbabilityInput);
    }

    public class MatchProbabilityService : IMatchProbabilityService
    {
        private readonly ICompressedPhenotypeExpander compressedPhenotypeExpander;
        private readonly IGenotypeLikelihoodService genotypeLikelihoodService;
        private readonly IGenotypeMatcher genotypeMatcher;
        private readonly IProbabilityCalculator probabilityCalculator;

        public MatchProbabilityService(
            ICompressedPhenotypeExpander compressedPhenotypeExpander,
            IGenotypeLikelihoodService genotypeLikelihoodService,
            IGenotypeMatcher genotypeMatcher,
            IProbabilityCalculator probabilityCalculator)
        {
            this.compressedPhenotypeExpander = compressedPhenotypeExpander;
            this.genotypeLikelihoodService = genotypeLikelihoodService;
            this.genotypeMatcher = genotypeMatcher;
            this.probabilityCalculator = probabilityCalculator;
        }

        public async Task<MatchProbabilityResponse> CalculateMatchProbability(MatchProbabilityInput matchProbabilityInput)
        {
            var patientGenotypes = 
                await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                    matchProbabilityInput.PatientHla,
                    matchProbabilityInput.HlaNomenclatureVersion);
            var donorGenotypes =
                await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                    matchProbabilityInput.DonorHla,
                    matchProbabilityInput.HlaNomenclatureVersion);

            var matchingPairs =
                (await genotypeMatcher.PairsWithTenOutOfTenMatch(
                    patientGenotypes,
                    donorGenotypes,
                    matchProbabilityInput.HlaNomenclatureVersion));

            // Returns early when patient/donor pair are guaranteed to either be a match or not
            if (patientGenotypes.Count * donorGenotypes.Count == matchingPairs.Count)
            {
                return new MatchProbabilityResponse {ZeroMismatchProbability = 1m};
            }
            if (matchingPairs.Count == 0)
            {
                return new MatchProbabilityResponse { ZeroMismatchProbability = 0m};
            }

            var genotypes = patientGenotypes.Union(donorGenotypes);
            var genotypesLikelihoods = (await Task.WhenAll(genotypes.Select(CalculateLikelihood))).ToDictionary();

            var probability = probabilityCalculator.CalculateMatchProbability(patientGenotypes, donorGenotypes, matchingPairs, genotypesLikelihoods);

            return new MatchProbabilityResponse{ZeroMismatchProbability = probability};
        }

        private async Task<KeyValuePair<PhenotypeInfo<string>, decimal>> CalculateLikelihood(PhenotypeInfo<string> genotype)
        {
            var likelihood = await genotypeLikelihoodService.CalculateLikelihood(genotype);
            return new KeyValuePair<PhenotypeInfo<string>, decimal>(genotype, likelihood);
        }
    }
}