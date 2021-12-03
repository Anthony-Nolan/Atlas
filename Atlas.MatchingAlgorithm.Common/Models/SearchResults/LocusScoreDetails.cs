using System;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;

namespace Atlas.MatchingAlgorithm.Common.Models.SearchResults
{
    public class LocusScoreDetails
    {
        public bool IsLocusTyped { get; set; }
        public LocusPositionScoreDetails ScoreDetailsAtPosition1 { get; set; }
        public LocusPositionScoreDetails ScoreDetailsAtPosition2 { get; set; }
        
        /// <summary>
        /// The aggregate overall match category for this locus, calculated from the individual position match grades
        /// </summary>
        public LocusMatchCategory? MatchCategory { get; set; }

        /// <summary>
        /// Indicates the direction of the DPB1 mismatch, when there is a DPB1 mismatch.
        /// When the mismatch is permissive or there is no DPB1 mismatch, NotApplicable will be returned.
        /// When the direction could not be calculated or the locus is non-DPB1, null will be returned.
        /// </summary>
        public Dpb1MismatchDirection? Dpb1MismatchDirection { get; set; }

        /// <summary>
        /// A numeric value representing the relative match grade at this locus, according to the scoring algorithm
        /// </summary>
        public int MatchGradeScore
        {
            get
            {
                if (ScoreDetailsAtPosition1.MatchGradeScore == null || ScoreDetailsAtPosition2.MatchGradeScore == null)
                {
                    throw new Exception("Cannot access grade score before it has been assigned - ensure that ranking has been performed on this result");
                }
                return (int) ScoreDetailsAtPosition1.MatchGradeScore + (int) ScoreDetailsAtPosition2.MatchGradeScore;
            }
        }

        /// <summary>
        /// A numeric value representing the relative match confidence at this locus, according to the scoring algorithm
        /// </summary>
        public int MatchConfidenceScore
        {
            get
            {
                if (ScoreDetailsAtPosition1?.MatchConfidenceScore == null || ScoreDetailsAtPosition2?.MatchConfidenceScore == null)
                {
                    throw new Exception("Cannot access confidence score before it has been assigned - ensure that ranking has been performed on this result");
                }
                return (int) ScoreDetailsAtPosition1.MatchConfidenceScore + (int) ScoreDetailsAtPosition2.MatchConfidenceScore;
            }
        }

        public bool IsPotentialMatch => ScoreDetailsAtPosition1.MatchConfidence == MatchConfidence.Potential &&
                                        ScoreDetailsAtPosition2.MatchConfidence == MatchConfidence.Potential;

        /// <summary>
        /// Calculates the match count based on the assigned grades. Used in the case where matching has not been run for a locus
        /// e.g. C and DQB1 in a 6/6 search
        /// </summary>
        public int MatchCount()
        {
            return new[]
                {
                    ScoreDetailsAtPosition1.MatchConfidence != MatchConfidence.Mismatch,
                    ScoreDetailsAtPosition2.MatchConfidence != MatchConfidence.Mismatch
                }.AsEnumerable()
                .Count(x => x);
        }
    }
}