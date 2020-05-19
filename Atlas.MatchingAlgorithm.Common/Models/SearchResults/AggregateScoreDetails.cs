using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults.PerLocus;

namespace Atlas.MatchingAlgorithm.Common.Models.SearchResults
{
    /// <summary>
    /// Data representing an overall assessment of a match, based on aggregate data across all individual loci. 
    /// </summary>
    public class AggregateScoreDetails
    {
        public int MatchCount { get; set; }
        public int PotentialMatchCount { get; set; }
        public int GradeScore { get; set; }
        public int ConfidenceScore { get; set; }
        public int TypedLociCount { get; set; }
        public MatchConfidence OverallMatchConfidence { get; set; }
        public MatchCategory MatchCategory { get; set; }
    }
}