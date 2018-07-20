using System;
using Newtonsoft.Json;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings
{
    public enum SequenceStatus
    {
        // enum values stored in db; do not change!
        Unknown = 0,
        Partial = 1,
        Full = 2
    }

    public enum DnaCategory
    {
        // enum values stored in db; do not change!
        Unknown = 0,
        CDna = 1,
        GDna = 2
    }

    public class AlleleTypingStatus : IEquatable<AlleleTypingStatus>
    {
        [JsonProperty("seq")]
        public SequenceStatus SequenceStatus { get; }

        [JsonProperty("dna")]
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
