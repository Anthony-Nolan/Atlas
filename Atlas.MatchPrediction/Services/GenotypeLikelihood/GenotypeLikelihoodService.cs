using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Client.Models.GenotypeLikelihood;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using HaplotypeHla = Atlas.Common.GeneticData.PhenotypeInfo.LociInfo<string>;

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
        private readonly IUnambiguousGenotypeExpander unambiguousGenotypeExpander;
        private readonly IGenotypeLikelihoodCalculator likelihoodCalculator;
        private readonly IGenotypeAlleleTruncater alleleTruncater;

        public GenotypeLikelihoodService(
            IHaplotypeFrequencySetRepository setRepository,
            IHaplotypeFrequenciesRepository frequencyRepository,
            IUnambiguousGenotypeExpander unambiguousGenotypeExpander,
            IGenotypeLikelihoodCalculator likelihoodCalculator,
            IGenotypeAlleleTruncater alleleTruncater)
        {
            this.setRepository = setRepository;
            this.frequencyRepository = frequencyRepository;
            this.unambiguousGenotypeExpander = unambiguousGenotypeExpander;
            this.likelihoodCalculator = likelihoodCalculator;
            this.alleleTruncater = alleleTruncater;
        }

        public async Task<GenotypeLikelihoodResponse> CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood)
        {
            var genotype = alleleTruncater.TruncateGenotypeAlleles(genotypeLikelihood.Genotype);

            var expandedGenotype = unambiguousGenotypeExpander.ExpandGenotype(genotype);
            var haplotypesWithFrequencies = await GetHaplotypesWithFrequencies(expandedGenotype);

            UpdateFrequenciesForDiplotype(haplotypesWithFrequencies, expandedGenotype.Diplotypes);
            var likelihood = likelihoodCalculator.CalculateLikelihood(expandedGenotype);

            return new GenotypeLikelihoodResponse {Likelihood = likelihood};
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