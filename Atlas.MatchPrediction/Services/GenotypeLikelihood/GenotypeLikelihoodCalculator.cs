using Atlas.MatchPrediction.Models;
using System.Linq;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeLikelihoodCalculator
    {
        /// <summary>
        /// Calculate genotype likelihood using the haplotype frequencies contained within a list of diplotypes.
        /// Provided diplotypes must have frequency values pre-populated, else said haplotype will be considered to have a frequency of 0.
        /// </summary>
        public decimal CalculateLikelihood(ImputedGenotype imputedGenotype);
    }

    public class GenotypeLikelihoodCalculator : IGenotypeLikelihoodCalculator
    {
        public decimal CalculateLikelihood(ImputedGenotype imputedGenotype)
        {
            var homozygosityCorrectionFactor = imputedGenotype.IsHomozygousAtEveryLocus ? 1 : 2;

            return imputedGenotype.Diplotypes.Sum(diplotype => CalculateDiplotypeLikelihood(diplotype, homozygosityCorrectionFactor));
        }

        private static decimal CalculateDiplotypeLikelihood(Diplotype diplotype, int homozygosityCorrectionFactor)
        {
            return diplotype.Item1.Frequency * diplotype.Item2.Frequency * homozygosityCorrectionFactor;
        }
    }
}