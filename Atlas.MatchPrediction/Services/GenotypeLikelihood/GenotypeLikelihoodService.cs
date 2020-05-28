using Atlas.MatchPrediction.Client.Models.GenotypeLikelihood;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeLikelihoodService
    {
        public GenotypeLikelihoodResponse CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood);
    }

    public class GenotypeLikelihoodService : IGenotypeLikelihoodService
    {
        private readonly IGenotypeImputation genotypeImputation;

        public GenotypeLikelihoodService(IGenotypeImputation genotypeImputation)
        {
            this.genotypeImputation = genotypeImputation;
        }

        public GenotypeLikelihoodResponse CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood)
        {
            var diplotypes = genotypeImputation.GetPossibleDiplotypes(genotypeLikelihood.Genotype);

            return new GenotypeLikelihoodResponse() { Likelihood = 1 };
        }
    }
}
