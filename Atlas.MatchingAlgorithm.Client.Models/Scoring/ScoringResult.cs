using Atlas.Client.Models.Search.Results.Matching.PerLocus;

namespace Atlas.MatchingAlgorithm.Client.Models.Scoring
{
    public class ScoringResult
    {
        /// <summary>
        /// The number of loci matched, down to the type.
        /// Out of a maximum of 10.
        /// Should some loci be untyped, then this field reflects the potential match count,
        /// rather than the actual known match count.
        /// </summary>
        public int TotalMatchCount { get; set; }

        /// <summary>
        /// The number of the total potential matches.
        /// The TotalMatchCount is a sum of potential and exact matches, so an exact match count can be calculated as the difference of these values
        /// </summary>
        public int PotentialMatchCount { get; set; }

        /// <summary>
        /// The number of the total exact matches.
        /// The TotalMatchCount is a sum of potential and exact matches, so an exact match count can be calculated as the difference of these values
        /// </summary>
        public int ExactMatchCount => TotalMatchCount - PotentialMatchCount;
        
        /// <summary>
        /// A numeric value representing the aggregate relative match grade across all loci, according to the scoring algorithm
        /// </summary>
        public int GradeScore { get; set; }

        /// <summary>
        /// A numeric value representing the aggregate relative match confidence across all loci, according to the scoring algorithm
        /// </summary>
        public int ConfidenceScore { get; set; }

        /// <summary>
        /// The overall confidence for the match
        /// </summary>
        public MatchConfidence OverallMatchConfidence { get; set; }
        
        /// <summary>
        /// The details of the match at locus A.
        /// </summary>
        public LocusSearchResult SearchResultAtLocusA { get; set; }

        /// <summary>
        /// The details of the match at locus B.
        /// </summary>
        public LocusSearchResult SearchResultAtLocusB { get; set; }

        /// <summary>
        /// The details of the match at locus C.
        /// </summary>
        public LocusSearchResult SearchResultAtLocusC { get; set; }

        /// <summary>
        /// The details of the match at locus DPB1.
        /// </summary>
        public LocusSearchResult SearchResultAtLocusDpb1 { get; set; }

        /// <summary>
        /// The details of the match at locus DQB1.
        /// </summary>
        public LocusSearchResult SearchResultAtLocusDqb1 { get; set; }

        /// <summary>
        /// The details of the match at locus DRB1.
        /// </summary>
        public LocusSearchResult SearchResultAtLocusDrb1 { get; set; }
    }
    
    
    /// <summary>
    /// Identified result of scoring of one donor against a patient
    /// </summary>
    public class DonorScoringResult
    {
        /// <summary>
        /// Identifier of the donor in the batch
        /// </summary>
        public string DonorId { get; set; }

        /// <summary>
        /// Scoring result of the donor
        /// </summary>
        public ScoringResult ScoringResult { get; set; }
    }
}