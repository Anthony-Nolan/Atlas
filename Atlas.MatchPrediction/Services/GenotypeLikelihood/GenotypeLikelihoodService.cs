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
        private readonly ILikelihoodCalculator likelihoodCalculator;

        public GenotypeLikelihoodService(
            IHaplotypeFrequencySetRepository setRepository,
            IHaplotypeFrequenciesRepository frequencyRepository,
            IGenotypeImputer genotypeImputer,
            ILikelihoodCalculator likelihoodCalculator)
        {
            this.setRepository = setRepository;
            this.frequencyRepository = frequencyRepository;
            this.genotypeImputer = genotypeImputer;
            this.likelihoodCalculator = likelihoodCalculator;
        }

        public async Task<GenotypeLikelihoodResponse> CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood)
        {
            var diplotypes = genotypeImputer.GetPossibleDiplotypes(genotypeLikelihood.Genotype);

            var haplotypes = GetHaplotypes(diplotypes).ToList();
            var frequencySet = await setRepository.GetActiveSet(null, null);
            var haplotypesWithFrequencies = await frequencyRepository.GetDiplotypeFrequencies(haplotypes, frequencySet.Id);

            UpdateFrequenciesForDiplotype(haplotypesWithFrequencies, diplotypes);
            var likelihood = likelihoodCalculator.CalculateLikelihood(diplotypes);

            return new GenotypeLikelihoodResponse {Likelihood = likelihood};
        }

        private static IEnumerable<LociInfo<string>> GetHaplotypes(IEnumerable<Diplotype> diplotypes)
        {
            return diplotypes.SelectMany(diplotype => new List<LociInfo<string>>
                {diplotype.Item1.Hla, diplotype.Item2.Hla});
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