using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Common.Models.Scoring
{
    public class MatchGradeResult
    {
        // The grade given to this match - it is the best grade calculated across all typing combinations
        public MatchGrade GradeResult;
        // The orientation(s) of the best grade calculated for this match.
        // It is an IEnumerable to account for the case of both orientations having a joint best match grade
        public IEnumerable<MatchOrientation> Orientations;
    }
}