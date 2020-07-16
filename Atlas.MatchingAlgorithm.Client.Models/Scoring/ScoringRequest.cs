using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Client.Models.Scoring
{
    public abstract class ScoringRequest
    {
        public PhenotypeInfo<string> PatientHla { get; set; }
        public IReadOnlyCollection<Locus> LociToScore { get; set; }
        public IReadOnlyCollection<Locus> LociToExcludeFromAggregateScoring { get; set; }
    }

    public class DonorHlaScoringRequest : ScoringRequest
    {
        public PhenotypeInfo<string> DonorHla { get; set; }
    }
}