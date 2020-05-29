using Atlas.MatchPrediction.Client.Models.GenotypeLikelihood;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeLikelihoodService
    {
        public GenotypeLikelihoodResponse CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood);
    }

    public class GenotypeLikelihoodService : IGenotypeLikelihoodService
    {
        private readonly IGenotypeImputer genotypeImputer;

        public GenotypeLikelihoodService(IGenotypeImputer genotypeImputer)
        {
            this.genotypeImputer = genotypeImputer;
        }

        public GenotypeLikelihoodResponse CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood)
        {
            var diplotypes = genotypeImputer.GetPossibleDiplotypes(genotypeLikelihood.Genotype);

            return new GenotypeLikelihoodResponse() { Likelihood = 1 };
        }
    }
}
