using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public interface IGenotypeLikelihoods
    {
        public Task<Dictionary<PhenotypeInfo<string>, decimal>> CalculateLikelihoods(
            ISet<PhenotypeInfo<string>> patientGenotypes,
            ISet<PhenotypeInfo<string>> donorGenotypes);
    }

    public class GenotypeLikelihoods : IGenotypeLikelihoods
    {
        private readonly IGenotypeLikelihoodService genotypeLikelihoodService;

        public GenotypeLikelihoods(IGenotypeLikelihoodService genotypeLikelihoodService)
        {
            this.genotypeLikelihoodService = genotypeLikelihoodService;
        }

        public async Task<Dictionary<PhenotypeInfo<string>, decimal>> CalculateLikelihoods(
            ISet<PhenotypeInfo<string>> patientGenotypes,
            ISet<PhenotypeInfo<string>> donorGenotypes)
        {
            var genotypes = patientGenotypes.Union(donorGenotypes);
            var genotypesLikelihoods = await Task.WhenAll(genotypes.Select(CalculateLikelihood));
            var genotypeLikelihoodDictionary = genotypesLikelihoods.ToDictionary(g => g.Key, g => g.Value);

            return genotypeLikelihoodDictionary;
        }

        private async Task<KeyValuePair<PhenotypeInfo<string>, decimal>> CalculateLikelihood(PhenotypeInfo<string> genotype)
        {
            var likelihood = await genotypeLikelihoodService.CalculateLikelihood(genotype);
            return new KeyValuePair<PhenotypeInfo<string>, decimal>(genotype, likelihood);
        }
    }
}