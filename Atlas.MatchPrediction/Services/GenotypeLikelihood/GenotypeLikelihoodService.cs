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
            var allMpaLoci = allowedLoci == LocusSettings.MatchPredictionLoci.ToHashSet();

            // Unrepresented haplotypes are assigned default value for decimal, 0 - which is what we want here.
            foreach (var diplotype in diplotypes)
            {
                haplotypesWithFrequencies.TryGetValue(diplotype.Item1.Hla, out var frequency1);
                if (!allMpaLoci && frequency1 == 0m)
                {
                    frequency1 = GetFrequencyForHla(haplotypesWithFrequencies, diplotype.Item1.Hla, allowedLoci);
                    haplotypesWithFrequencies.Add(diplotype.Item1.Hla, frequency1);
                }

                haplotypesWithFrequencies.TryGetValue(diplotype.Item2.Hla, out var frequency2);
                if (!allMpaLoci && frequency2 == 0m)
                {
                    frequency1 = GetFrequencyForHla(haplotypesWithFrequencies, diplotype.Item1.Hla, allowedLoci);
                    haplotypesWithFrequencies.Add(diplotype.Item1.Hla, frequency1);
                }

                diplotype.Item1.Frequency = frequency1;
                diplotype.Item2.Frequency = frequency2;
            }
        }

        private static decimal GetFrequencyForHla(
            IReadOnlyDictionary<HaplotypeHla, decimal> haplotypesWithFrequencies,
            HaplotypeHla hla,
            ISet<Locus> allowedLoci)
        {
            return haplotypesWithFrequencies
                .Where(kvp => kvp.Key.EqualsAtLoci(hla, allowedLoci)).Select(kvp => kvp.Value)
                .DefaultIfEmpty(0m)
                .Sum();
        }
    }
}