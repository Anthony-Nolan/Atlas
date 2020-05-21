using System;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;

namespace Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup
{
    public class HlaScoringLookupResult : 
        IHlaScoringLookupResult, 
        IEquatable<HlaScoringLookupResult>
    {
        public Locus Locus { get; }
        public string LookupName { get; }

        public TypingMethod TypingMethod => HlaTypingCategory == HlaTypingCategory.Serology
            ? TypingMethod.Serology
            : TypingMethod.Molecular;

        public HlaTypingCategory HlaTypingCategory { get; }
        public IHlaScoringInfo HlaScoringInfo { get; }
        public object HlaInfoToSerialise => HlaScoringInfo;

        public HlaScoringLookupResult(
            Locus locus,
            string lookupName,
            HlaTypingCategory hlaTypingCategory,
            IHlaScoringInfo hlaScoringInfo)
        {
            Locus = locus;
            LookupName = lookupName;
            HlaTypingCategory = hlaTypingCategory;
            HlaScoringInfo = hlaScoringInfo;
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return new HlaLookupTableEntity(this)
            {
                HlaTypingCategoryAsString = HlaTypingCategory.ToString() //QQ needs attention for rename
            };
        }

        public bool Equals(HlaScoringLookupResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                Locus == other.Locus && 
                string.Equals(LookupName, other.LookupName) && 
                TypingMethod == other.TypingMethod && 
                HlaTypingCategory == other.HlaTypingCategory && 
                HlaScoringInfo.Equals(other.HlaScoringInfo);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HlaScoringLookupResult) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) Locus;
                hashCode = (hashCode * 397) ^ LookupName.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) TypingMethod;
                hashCode = (hashCode * 397) ^ (int) HlaTypingCategory;
                hashCode = (hashCode * 397) ^ HlaScoringInfo.GetHashCode();
                return hashCode;
            }
        }
    }
}
