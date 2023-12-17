using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Common.Models.Scoring
{
    public class LocusScoreResult<T> : IEquatable<LocusScoreResult<T>>
    {
        public LocusInfo<T> LocusScore { get; set; }

        /// <summary>
        /// The orientation(s) of the best result calculated for the <see cref="LocusScore"/>.
        /// It is a collection to account for the case of both orientations having a joint best score.
        /// </summary>
        public IEnumerable<MatchOrientation> Orientations { get; set; }

        public LocusScoreResult()
        {
        }

        public LocusScoreResult(LocusInfo<T> locusScore, IEnumerable<MatchOrientation> orientations)
        {
            LocusScore = locusScore;
            Orientations = orientations;
        }

        /// <summary>
        /// <see cref="LocusScoreResult{T}"/> constructor for when same <paramref name="locusScore"/> to be applied
        /// to both positions of <see cref="LocusScore"/> in both possible <see cref="Orientations"/>.
        /// </summary>
        public LocusScoreResult(T locusScore)
        {
            LocusScore = new LocusInfo<T>(locusScore);
            Orientations = new [] { MatchOrientation.Direct, MatchOrientation.Cross};
        }

        #region Equality members

        /// <inheritdoc />
        public bool Equals(LocusScoreResult<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(LocusScore, other.LocusScore) && Orientations.SequenceEqual(other.Orientations);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LocusScoreResult<T>)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(LocusScore, Orientations);
        }

        #endregion
    }

    public static class LocusScoreResultExtensions
    {
        public static LociInfo<IEnumerable<MatchOrientation>> GetMatchOrientations<T>(this LociInfo<LocusScoreResult<T>> locusScoreResults)
        {
            return locusScoreResults.Map(locusResult => locusResult?.Orientations ?? new List<MatchOrientation>());
        }
    }
}