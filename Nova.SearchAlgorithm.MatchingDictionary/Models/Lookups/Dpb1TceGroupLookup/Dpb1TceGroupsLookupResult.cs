using System;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.Dpb1TceGroupLookup
{
    public interface IDpb1TceGroupsLookupResult : IHlaLookupResult
    {
        string TceGroup { get; }
    }

    public class Dpb1TceGroupsLookupResult : IEquatable<IDpb1TceGroupsLookupResult>, IDpb1TceGroupsLookupResult
    {
        public MatchLocus MatchLocus => MatchLocus.Dpb1;
        public string LookupName { get; }
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public string TceGroup { get; }
        public object HlaInfoToSerialise => TceGroup;

        public Dpb1TceGroupsLookupResult(
            string lookupName,
            string tceGroup)
        {
            LookupName = lookupName;
            TceGroup = tceGroup;
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return new HlaLookupTableEntity(this);
        }

        public bool Equals(IDpb1TceGroupsLookupResult other)
        {
            return string.Equals(LookupName, other.LookupName) && string.Equals(TceGroup, other.TceGroup);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((IDpb1TceGroupsLookupResult) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (LookupName.GetHashCode() * 397) ^ TceGroup.GetHashCode();
            }
        }
    }
}
