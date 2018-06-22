using System;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings
{
    public enum SequenceStatus
    {
        Unknown,
        Partial,
        Full
    }

    public enum DnaCategory
    {
        Unknown,
        CDna,
        GDna
    }

    public class AlleleTypingStatus : IEquatable<AlleleTypingStatus>
    {
        public SequenceStatus SequenceStatus { get; }
        public DnaCategory DnaCategory { get; }

        public AlleleTypingStatus(SequenceStatus sequenceStatus, DnaCategory dnaCategory)
        {
            SequenceStatus = sequenceStatus;
            DnaCategory = dnaCategory;
        }

        private AlleleTypingStatus() : this(SequenceStatus.Unknown, DnaCategory.Unknown)
        {
        }

        public static AlleleTypingStatus GetDefaultStatus()
        {
            return new AlleleTypingStatus();
        }

        public bool Equals(AlleleTypingStatus other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return SequenceStatus == other.SequenceStatus && DnaCategory == other.DnaCategory;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AlleleTypingStatus) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) SequenceStatus * 397) ^ (int) DnaCategory;
            }
        }
    }
}
