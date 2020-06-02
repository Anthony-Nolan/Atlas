using System.Collections.Generic;
using Atlas.MatchPrediction.Models;
using System.Linq;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface ILikelihoodCalculator
    {
        public decimal CalculateLikelihood(List<Diplotype> diplotypes);
    }

    public class LikelihoodCalculator : ILikelihoodCalculator
    {
        public decimal CalculateLikelihood(List<Diplotype> diplotypes)
        {
            var diplotypeLikelihoods = CalculateDiplotypeLikelihoods(diplotypes);
            return diplotypeLikelihoods.Sum();
        }

        private static IEnumerable<decimal> CalculateDiplotypeLikelihoods(List<Diplotype> diplotypes)
        {
            var correctionFactor = diplotypes.Count == 1 && diplotypes[0].Item1.Hla == diplotypes[0].Item2.Hla ? 1 : 2;

            return diplotypes.Select(diplotype =>
                diplotype.Item1.Frequency * diplotype.Item2.Frequency * correctionFactor);
        }
    }
}