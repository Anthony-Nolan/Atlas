using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using HaplotypeHla = Atlas.Common.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeLikelihoodService
    {
        public Task<decimal> CalculateLikelihood(PhenotypeInfo<string> genotype);
    }

    internal class GenotypeLikelihoodService : IGenotypeLikelihoodService
    {
        private readonly IHaplotypeFrequencySetRepository setRepository;
        private readonly IHaplotypeFrequenciesRepository frequencyRepository;
        private readonly IUnambiguousGenotypeExpander unambiguousGenotypeExpander;
        private readonly IGenotypeLikelihoodCalculator likelihoodCalculator;

        public GenotypeLikelihoodService(
            IHaplotypeFrequencySetRepository setRepository,
            IHaplotypeFrequenciesRepository frequencyRepository,
            IUnambiguousGenotypeExpander unambiguousGenotypeExpander,
            IGenotypeLikelihoodCalculator likelihoodCalculator)
        {
            this.setRepository = setRepository;
            this.frequencyRepository = frequencyRepository;
            this.unambiguousGenotypeExpander = unambiguousGenotypeExpander;
            this.likelihoodCalculator = likelihoodCalculator;
        }

        public async Task<decimal> CalculateLikelihood(PhenotypeInfo<string> genotype)
        {
            var expandedGenotype = unambiguousGenotypeExpander.ExpandGenotype(genotype);
            var haplotypesWithFrequencies = await GetHaplotypesWithFrequencies(expandedGenotype);

            UpdateFrequenciesForDiplotype(haplotypesWithFrequencies, expandedGenotype.Diplotypes);
            return likelihoodCalculator.CalculateLikelihood(expandedGenotype);
        }

        private async Task<Dictionary<HaplotypeHla, decimal>> GetHaplotypesWithFrequencies(ExpandedGenotype expandedGenotype)
        {
            var haplotypes = GetHaplotypes(expandedGenotype.Diplotypes);
            var frequencySet = await setRepository.GetActiveSet(null, null);

            return await frequencyRepository.GetHaplotypeFrequencies(haplotypes, frequencySet.Id);
        }

        public IEnumerable<HaplotypeHla> GetHaplotypes(IEnumerable<Diplotype> diplotypes)
        {
            return diplotypes.SelectMany(diplotype => new List<HaplotypeHla> { diplotype.Item1.Hla, diplotype.Item2.Hla });
        }

        private static void UpdateFrequenciesForDiplotype(
            Dictionary<HaplotypeHla, decimal> haplotypesWithFrequencies,
            IEnumerable<Diplotype> diplotypes)
        {
            foreach (var diplotype in diplotypes)
            {
                diplotype.Item1.Frequency = haplotypesWithFrequencies[diplotype.Item1.Hla];
                diplotype.Item2.Frequency = haplotypesWithFrequencies[diplotype.Item2.Hla];
            }
        }
    }
}