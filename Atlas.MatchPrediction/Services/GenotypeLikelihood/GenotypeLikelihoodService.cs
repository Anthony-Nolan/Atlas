using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeLikelihoodService
    {
        public decimal CalculateLikelihood(
            PhenotypeInfo<string> genotype,
            ISet<Locus> allowedLoci,
            ConcurrentDictionary<LociInfo<string>, HaplotypeFrequency> haplotypeFrequencies);
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

        public decimal CalculateLikelihood(
            PhenotypeInfo<string> genotype,
            ISet<Locus> allowedLoci,
            ConcurrentDictionary<LociInfo<string>, HaplotypeFrequency> haplotypeFrequencies)
        {
            var expandedGenotype = unambiguousGenotypeExpander.ExpandGenotype(genotype, allowedLoci);
            var excludedLoci = LocusSettings.MatchPredictionLoci.Except(allowedLoci).ToHashSet();
            
            foreach (var diplotype in expandedGenotype.Diplotypes)
            {
                diplotype.Item1.Frequency = haplotypeFrequencyService.GetFrequencyForHla(diplotype.Item1.Hla, excludedLoci, haplotypeFrequencies);
                diplotype.Item2.Frequency = haplotypeFrequencyService.GetFrequencyForHla(diplotype.Item2.Hla, excludedLoci, haplotypeFrequencies);
            }

            return likelihoodCalculator.CalculateLikelihood(expandedGenotype);
        }
    }
}