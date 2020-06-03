using System.Collections.Generic;
using Atlas.MatchPrediction.Models;
using System.Linq;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface ILikelihoodCalculator
    {
        // CalculateLikelihood is calculating the likelihood of a genotype from a list of diplotypes
        // and their haplotypes corresponding frequencies.
        // We expect the frequency to be pre-populated on the input diplotypes.
        public decimal CalculateLikelihood(List<Diplotype> diplotypes);
    }

    public class LikelihoodCalculator : ILikelihoodCalculator
    {
        public decimal CalculateLikelihood(List<Diplotype> diplotypes)
        {
            var homozygosityCorrectionFactor = GetHomozygosityCorrectionFactor(diplotypes);

            var diplotypeLikelihoods =
                diplotypes.Select(diplotype => CalculateDiplotypeLikelihood(diplotype, homozygosityCorrectionFactor));

            return diplotypeLikelihoods.Sum();
        }

        private static decimal CalculateDiplotypeLikelihood(Diplotype diplotype, int homozygosityCorrectionFactor)
        {
            return diplotype.Item1.Frequency * diplotype.Item2.Frequency * homozygosityCorrectionFactor;
        }

        private static int GetHomozygosityCorrectionFactor(List<Diplotype> diplotypes)
        {
            return diplotypes.Count == 1 && diplotypes[0].Item1.Hla == diplotypes[0].Item2.Hla ? 1 : 2;
        }
    }
}