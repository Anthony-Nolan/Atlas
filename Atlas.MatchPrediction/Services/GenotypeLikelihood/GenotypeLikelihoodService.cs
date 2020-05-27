using Atlas.MatchPrediction.Client.Models.GenotypeLikelihood;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeLikelihoodService
    {
        public GenotypeLikelihoodResponse CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood);
    }

    public class GenotypeLikelihoodService : IGenotypeLikelihoodService
    {
        private readonly ISplitGenotype splitGenotype;

        public GenotypeLikelihoodService(ISplitGenotype splitGenotype)
        {
            this.splitGenotype = splitGenotype;
        }

        public GenotypeLikelihoodResponse CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood)
        {
            var diplotypes = splitGenotype.SplitIntoDiplotypes(genotypeLikelihood.Genotype);

            return new GenotypeLikelihoodResponse() { Likelihood = 1 };
        }
    }
}
