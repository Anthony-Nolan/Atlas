using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchPredictionSteps.GenotypeLikelihood
{
    public class GenotypeLikelihoodInput
    {
        public PhenotypeInfo<string> Genotype { get; set; }
        public FrequencySetMetadata FrequencySetMetaData { get; set; }
        public ISet<Locus> AllowedLoci { get; set; }
    }
}