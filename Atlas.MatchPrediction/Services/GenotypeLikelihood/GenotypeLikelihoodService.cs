using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Client.Models.GenotypeLikelihood;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeLikelihoodService
    {
        public Task<GenotypeLikelihoodResponse> CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood);
    }

    public class GenotypeLikelihoodService : IGenotypeLikelihoodService
    {
        private readonly IHaplotypeFrequencySetRepository setRepository;
        private readonly IHaplotypeFrequenciesRepository frequencyRepository;
        private readonly IGenotypeImputer genotypeImputer;
        private readonly IGenotypeLikelihoodCalculator likelihoodCalculator;

        public GenotypeLikelihoodService(
            IHaplotypeFrequencySetRepository setRepository,
            IHaplotypeFrequenciesRepository frequencyRepository,
            IGenotypeImputer genotypeImputer,
            IGenotypeLikelihoodCalculator likelihoodCalculator)
        {
            this.setRepository = setRepository;
            this.frequencyRepository = frequencyRepository;
            this.genotypeImputer = genotypeImputer;
            this.likelihoodCalculator = likelihoodCalculator;
        }

        public async Task<GenotypeLikelihoodResponse> CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood)
        {
            var imputedGenotype = genotypeImputer.ImputeGenotype(genotypeLikelihood.Genotype);
            var haplotypesWithFrequencies = await GetHaplotypesWithFrequencies(imputedGenotype);

            UpdateFrequenciesForDiplotype(haplotypesWithFrequencies, imputedGenotype.Diplotypes);
            var likelihood = likelihoodCalculator.CalculateLikelihood(imputedGenotype);

            return new GenotypeLikelihoodResponse {Likelihood = likelihood};
        }

        private async Task<Dictionary<LociInfo<string>, decimal>> GetHaplotypesWithFrequencies(ImputedGenotype imputedGenotype)
        {
            var haplotypes = imputedGenotype.GetHaplotypes().ToList();
            var frequencySet = await setRepository.GetActiveSet(null, null);

            return await frequencyRepository.GetDiplotypeFrequencies(haplotypes, frequencySet.Id);
        }

        private static void UpdateFrequenciesForDiplotype(
            Dictionary<LociInfo<string>, decimal> haplotypesWithFrequencies, IEnumerable<Diplotype> diplotypes)
        {
            foreach (var diplotype in diplotypes)
            {
                diplotype.Item1.Frequency = haplotypesWithFrequencies[diplotype.Item1.Hla];
                diplotype.Item2.Frequency = haplotypesWithFrequencies[diplotype.Item2.Hla];
            }
        }
    }
}