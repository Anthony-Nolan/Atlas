using Atlas.Common.GeneticData.Hla.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata
{
    /// <summary>
    /// Metadata required to score HLA pairings.
    /// </summary>
    public interface IHlaScoringMetadata : ISerialisableHlaMetadata
    {
        IHlaScoringInfo HlaScoringInfo { get; }
        IEnumerable<IHlaScoringMetadata> GetInTermsOfSingleAlleleScoringMetadata();
    }

    internal class HlaScoringMetadata :
        SerialisableHlaMetadata,
        IHlaScoringMetadata,
        IEquatable<HlaScoringMetadata>
    {
        public IHlaScoringInfo HlaScoringInfo { get; }
        public override object HlaInfoToSerialise => HlaScoringInfo;

        internal HlaScoringMetadata(
            Locus locus,
            string lookupName,
            IHlaScoringInfo hlaScoringInfo,
            TypingMethod typingMethod)
            : base(locus, lookupName, typingMethod)
        {
            HlaScoringInfo = hlaScoringInfo;
        }

        public IEnumerable<IHlaScoringMetadata> GetInTermsOfSingleAlleleScoringMetadata()
        {
            return HlaScoringInfo.ConvertToSingleAllelesInfo().Select(info => new HlaScoringMetadata(
                Locus,
                info.AlleleName,
                info,
                TypingMethod
            ));
        }

        #region IEquatable
        public bool Equals(HlaScoringMetadata other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return
                Locus == other.Locus &&
                string.Equals(LookupName, other.LookupName) &&
                TypingMethod == other.TypingMethod &&
                HlaScoringInfo.Equals(other.HlaScoringInfo);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HlaScoringMetadata)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Locus;
                hashCode = (hashCode * 397) ^ LookupName.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)TypingMethod;
                hashCode = (hashCode * 397) ^ HlaScoringInfo.GetHashCode();
                return hashCode;
            }
        }
        #endregion
    }
}
