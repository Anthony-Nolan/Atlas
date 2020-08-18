using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;

namespace Atlas.MatchingAlgorithm.Common.Models.Scoring
{
    public class MatchGradeResult : IEquatable<MatchGradeResult>
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

        public bool Equals(MatchGradeResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                GradeResult == other.GradeResult && 
                Orientations.SequenceEqual(other.Orientations);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MatchGradeResult) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) GradeResult * 397) ^ Orientations.GetHashCode();
            }
        }
    }
}