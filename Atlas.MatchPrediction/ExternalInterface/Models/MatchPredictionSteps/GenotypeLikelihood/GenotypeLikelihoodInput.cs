﻿using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchPredictionSteps.GenotypeLikelihood
{
    public class GenotypeLikelihoodInput
    {
        public PhenotypeInfo<string> Genotype { get; set; }
        public FrequencySetMetadata FrequencySetMetaData { get; set; }
        public ISet<Locus> AllowedLoci { get; set; }
    }
}