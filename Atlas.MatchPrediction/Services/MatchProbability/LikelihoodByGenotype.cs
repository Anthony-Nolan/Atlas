using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public interface ILikelihoodByGenotype
    {
        public Task<Dictionary<PhenotypeInfo<string>, decimal>> CreateLikelihoodGenotypeDictionary(
            ISet<PhenotypeInfo<string>> patientGenotypes,
            ISet<PhenotypeInfo<string>> donorGenotypes);
    }

    public class ByLikelihoodByGenotype : ILikelihoodByGenotype
    {
        private readonly IGenotypeLikelihoodService genotypeLikelihoodService;

        public ByLikelihoodByGenotype(IGenotypeLikelihoodService genotypeLikelihoodService)
        {
            this.genotypeLikelihoodService = genotypeLikelihoodService;
        }

        public async Task<Dictionary<PhenotypeInfo<string>, decimal>> CreateLikelihoodGenotypeDictionary(
            ISet<PhenotypeInfo<string>> patientGenotypes,
            ISet<PhenotypeInfo<string>> donorGenotypes)
        {
            var genotypes = patientGenotypes.Union(donorGenotypes);
            var genotypesLikelihoods = await Task.WhenAll(genotypes.Select(CalculateLikelihood));
            return genotypesLikelihoods.ToDictionary();
        }

        private async Task<KeyValuePair<PhenotypeInfo<string>, decimal>> CalculateLikelihood(PhenotypeInfo<string> genotype)
        {
            var likelihood = await genotypeLikelihoodService.CalculateLikelihood(genotype);
            return new KeyValuePair<PhenotypeInfo<string>, decimal>(genotype, likelihood);
        }
    }
}