using Atlas.MatchPrediction.Client.Models.GenotypeLikelihood;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeLikelihoodService
    {
        public GenotypeLikelihoodResponse CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood);
    }

    public class GenotypeLikelihoodService : IGenotypeLikelihoodService
    {
        public GenotypeLikelihoodResponse CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood)
        {

            return new GenotypeLikelihoodResponse() { Likelihood = 1 };
        }
    }
}
