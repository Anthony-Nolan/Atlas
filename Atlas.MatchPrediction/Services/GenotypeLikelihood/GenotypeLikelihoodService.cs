using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Config;
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
            var expandedGenotype = unambiguousGenotypeExpander.ExpandGenotype(genotype, allowedLoci);
            var haplotypesWithFrequencies = await haplotypeFrequencyService.GetAllHaplotypeFrequencies(frequencySet.Id);

            UpdateFrequenciesForDiplotype(haplotypesWithFrequencies, expandedGenotype.Diplotypes, allowedLoci);
            return likelihoodCalculator.CalculateLikelihood(expandedGenotype);
        }

        private static void UpdateFrequenciesForDiplotype(
            Dictionary<HaplotypeHla, decimal> haplotypesWithFrequencies,
            IEnumerable<Diplotype> diplotypes,
            ISet<Locus> allowedLoci)
        {
            // Unrepresented haplotypes are assigned default value for decimal, 0 - which is what we want here.
            foreach (var diplotype in diplotypes)
            {
                diplotype.Item1.Frequency = GetFrequencyForHla(haplotypesWithFrequencies, diplotype.Item1.Hla, allowedLoci);
                diplotype.Item2.Frequency = GetFrequencyForHla(haplotypesWithFrequencies, diplotype.Item2.Hla, allowedLoci);
            }
        }

        private static decimal GetFrequencyForHla(
            Dictionary<HaplotypeHla, decimal> haplotypesWithFrequencies,
            HaplotypeHla hla,
            ISet<Locus> allowedLoci)
        {
            haplotypesWithFrequencies.TryGetValue(hla, out var frequency);

            if (allowedLoci != LocusSettings.MatchPredictionLoci.ToHashSet() && frequency == default)
            {
                frequency = haplotypesWithFrequencies
                    .Where(kvp => kvp.Key.EqualsAtLoci(hla, allowedLoci)).Select(kvp => kvp.Value)
                    .DefaultIfEmpty(0m)
                    .Sum();

                haplotypesWithFrequencies.Add(hla, frequency);
            }

            return frequency;
        }
    }
}