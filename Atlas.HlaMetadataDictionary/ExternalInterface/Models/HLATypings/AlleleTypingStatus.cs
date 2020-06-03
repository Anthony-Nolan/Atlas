using System;
using Newtonsoft.Json;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings
{
    public enum SequenceStatus
    {
        // Enum values stored in db; changing values will require rebuild
        // of the Metadata Dictionary storage tables
        Unknown = 0,
        Partial = 1,
        Full = 2
    }

    public enum DnaCategory
    {
        // Enum values stored in db; changing values will require rebuild
        // of the Metadata Dictionary storage tables.
        Unknown = 0,
        CDna = 1,
        GDna = 2
    }

    public class AlleleTypingStatus : IEquatable<AlleleTypingStatus>
    {
        // Shortened property names are used when serialising the object for storage
        // to reduce the total row size

        [JsonProperty("seq")]
        public SequenceStatus SequenceStatus { get; }

        [JsonProperty("dna")]
        public DnaCategory DnaCategory { get; }

        [JsonConstructor]
        internal AlleleTypingStatus(SequenceStatus sequenceStatus, DnaCategory dnaCategory)
        {
            if (sequenceStatus == SequenceStatus.Unknown ^ dnaCategory == DnaCategory.Unknown)
            {
                throw new ArgumentException("Both sequence status and dna category must be set to unknown; or neither must be set to unknown.");
            }

            SequenceStatus = sequenceStatus;
            DnaCategory = dnaCategory;
        }

        internal static AlleleTypingStatus GetDefaultStatus()
        {
            return new AlleleTypingStatus(SequenceStatus.Unknown, DnaCategory.Unknown);
        }

        #region IEquatable
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
        #endregion
    }
}
