using System;
using System.Collections.Generic;
using System.Text;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeLikelihoodService
    {
        public decimal CalculateLikelihood(PhenotypeInfo<string> genotype);
    }

    public class GenotypeLikelihoodService : IGenotypeLikelihoodService
    {
        public decimal CalculateLikelihood(PhenotypeInfo<string> genotype)
        {
            return 1;
        }
    }
}
