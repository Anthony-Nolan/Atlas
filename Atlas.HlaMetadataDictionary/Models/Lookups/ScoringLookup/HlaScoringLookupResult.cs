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

        /// <param name="locus">Locus</param>
        /// <param name="lookupName">Allele Descriptor</param>
        /// <param name="hlaTypingCategory">
        /// This parameter is used for 2 purposes:
        ///  * Indicating whether the data is Serological or Molecular
        ///  * Indicating the nature of data to be serialised to AzureStorage
        /// If this entity won't be serialised then it doesn't matter which of the Molecular values is provided.
        /// We default to 'AlleleStringOfNames' because it's NOT supported as a serialisation type, thus
        /// inaccurate usages will cause noisy errors (rather than silent after-the-fact errors)
        /// </param>
        /// <param name="hlaScoringInfo">Data</param>
        public HlaScoringLookupResult(
            Locus locus,
            string lookupName,
            IHlaScoringInfo hlaScoringInfo,
            HlaTypingCategory hlaTypingCategory = HlaTypingCategory.AlleleStringOfNames)
        {
            Locus = locus;
            LookupName = lookupName;
            HlaTypingCategory = hlaTypingCategory;
            HlaScoringInfo = hlaScoringInfo;
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return new HlaLookupTableEntity(this, HlaTypingCategory);
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
