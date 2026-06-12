using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeLikelihoodService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="genotype">
        /// string HLA data must be at the same typing resolution as the frequency set provided, as no conversions will take place as part of this lookup.
        ///
        /// "Genotype" means that this should be used when calculating the likelihood of a genotype without known phase - meaning that even using unambiguous
        /// allele data here will still result in expansion to up to 16 diplotypes, via variation of phase within the genotype. 
        /// </param>
        /// <param name="frequencySet"></param>
        /// <param name="allowedLoci"></param>
        /// <returns></returns>
        public Task<decimal> CalculateLikelihoodForGenotype(
            PhenotypeInfo<string> genotype,
            HaplotypeFrequencySet frequencySet,
            ISet<Locus> allowedLoci);

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

        /// <inheritdoc />
        public async Task<decimal> CalculateLikelihoodForGenotype(
            PhenotypeInfo<string> genotype,
            HaplotypeFrequencySet frequencySet,
            ISet<Locus> allowedLoci)
        {
            var expandedGenotype = unambiguousGenotypeExpander.ExpandGenotype(genotype, allowedLoci);
            var excludedLoci = LocusSettings.MatchPredictionLoci.Except(allowedLoci).ToHashSet();

            foreach (var diplotype in expandedGenotype.Diplotypes)
            {
                diplotype.Item1.Frequency = await haplotypeFrequencyService.GetFrequencyForHla(frequencySet.Id, diplotype.Item1.Hla, excludedLoci);
                diplotype.Item2.Frequency = await haplotypeFrequencyService.GetFrequencyForHla(frequencySet.Id, diplotype.Item2.Hla, excludedLoci);
            }

            return likelihoodCalculator.CalculateLikelihood(expandedGenotype);
        }

    }
}