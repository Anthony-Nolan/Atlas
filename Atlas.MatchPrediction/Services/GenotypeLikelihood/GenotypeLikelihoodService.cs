using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using HaplotypeHla = Atlas.Common.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeLikelihoodService
    {
        public Task<decimal> CalculateLikelihood(PhenotypeInfo<string> genotype, HaplotypeFrequencySet frequencySet, ISet<Locus> allowedLoci);
    }

    internal class GenotypeLikelihoodService : IGenotypeLikelihoodService
    {
        private readonly IUnambiguousGenotypeExpander unambiguousGenotypeExpander;
        private readonly IGenotypeLikelihoodCalculator likelihoodCalculator;
        private readonly IHaplotypeFrequencyService haplotypeFrequencyService;

        public GenotypeLikelihoodService(
            IUnambiguousGenotypeExpander unambiguousGenotypeExpander,
            IGenotypeLikelihoodCalculator likelihoodCalculator,
            IHaplotypeFrequencyService haplotypeFrequencyService
        )
        {
            this.unambiguousGenotypeExpander = unambiguousGenotypeExpander;
            this.likelihoodCalculator = likelihoodCalculator;
            this.haplotypeFrequencyService = haplotypeFrequencyService;
        }

        public async Task<decimal> CalculateLikelihood(PhenotypeInfo<string> genotype, HaplotypeFrequencySet frequencySet, ISet<Locus> allowedLoci)
        {
            var expandedGenotype = unambiguousGenotypeExpander.ExpandGenotype(genotype);
            var haplotypesWithFrequencies = await GetHaplotypesWithFrequencies(expandedGenotype, frequencySet);

            UpdateFrequenciesForDiplotype(haplotypesWithFrequencies, expandedGenotype.Diplotypes);
            return likelihoodCalculator.CalculateLikelihood(expandedGenotype);
        }

        private async Task<Dictionary<HaplotypeHla, decimal>> GetHaplotypesWithFrequencies(
            ExpandedGenotype expandedGenotype,
            HaplotypeFrequencySet haplotypeFrequencySet
        )
        {
            var haplotypes = GetHaplotypes(expandedGenotype.Diplotypes);
            return await haplotypeFrequencyService.GetAllHaplotypeFrequencies(haplotypeFrequencySet.Id);
        }

        private static IEnumerable<HaplotypeHla> GetHaplotypes(IEnumerable<Diplotype> diplotypes)
        {
            return diplotypes.SelectMany(diplotype => new List<HaplotypeHla> {diplotype.Item1.Hla, diplotype.Item2.Hla});
        }

        private static void UpdateFrequenciesForDiplotype(
            IReadOnlyDictionary<HaplotypeHla, decimal> haplotypesWithFrequencies,
            IEnumerable<Diplotype> diplotypes)
        {
            foreach (var diplotype in diplotypes)
            {
                // Unrepresented haplotypes are assigned default value for decimal, 0 - which is what we want here.
                haplotypesWithFrequencies.TryGetValue(diplotype.Item1.Hla, out var frequency1);
                haplotypesWithFrequencies.TryGetValue(diplotype.Item2.Hla, out var frequency2);

                diplotype.Item1.Frequency = frequency1;
                diplotype.Item2.Frequency = frequency2;
            }
        }
    }
}