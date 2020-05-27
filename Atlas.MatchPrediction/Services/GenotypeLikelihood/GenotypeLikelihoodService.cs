using System;
using System.Collections.Generic;
using System.Text;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Client.Models.GenotypeLikelihood;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeLikelihoodService
    {
        public decimal CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood);
    }

    public class GenotypeLikelihoodService : IGenotypeLikelihoodService
    {
        public decimal CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood)
        {
            return 1;
        }
    }
}
