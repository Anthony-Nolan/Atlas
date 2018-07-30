using System.Collections.Generic;
using Nova.SearchAlgorithm.Client.Models.SearchResults;

namespace Nova.SearchAlgorithm.Common.Models.Scoring
{
    public class MatchGradeResult
    {
        /// <summary>
        /// The grade given to this match - it is the best grade calculated across all typing combinations
        /// </summary>
        public MatchGrade GradeResult { get; set; }

        /// <summary>
        /// The orientation(s) of the best grade calculated for this match.
        /// It is an IEnumerable to account for the case of both orientations having a joint best match grade
        /// </summary>
        public IEnumerable<MatchOrientation> Orientations { get; set; }

        public MatchGradeResult()
        {
        }

        public MatchGradeResult(MatchGrade gradeResult, IEnumerable<MatchOrientation> orientations)
        {
            GradeResult = gradeResult;
            Orientations = orientations;
        }
    }
}