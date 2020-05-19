using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Atlas.HlaMetadataDictionary.Models.HLATypings;

namespace Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup
{
    /// <summary>
    /// To be used with allele name variants that map to >1 allele,
    /// where info on each individual allele represented by the typing is needed for scoring.
    /// </summary>
    internal class MultipleAlleleScoringInfo : 
        IHlaScoringInfo, 
        IEquatable<MultipleAlleleScoringInfo>
    {
        /// <summary>
        /// The scoring info for each single allele represented by this typing only holds molecular data;
        /// this is to reduce the object's final row size. The consolidated serology data
        /// needed when scoring against a serology typing is held in the Matching Serologies property.
        /// </summary>
        public IEnumerable<SingleAlleleScoringInfo> AlleleScoringInfos { get; }

        /// <inheritdoc />
        /// <summary>
        /// The total collection of serologies that match the multiple allele typing
        /// is passed in during object creation; this data will be stored in the cloud table.
        /// </summary>
        public IEnumerable<SerologyEntry> MatchingSerologies { get; }

        [JsonIgnore]
        public IEnumerable<string> MatchingGGroups => AlleleScoringInfos
            .Select(info => info.MatchingGGroup)
            .Distinct();

        [JsonIgnore]
        public IEnumerable<string> MatchingPGroups => AlleleScoringInfos
            .Select(info => info.MatchingPGroup)
            .Where(pGroup => pGroup != null)
            .Distinct();

        public MultipleAlleleScoringInfo(
            IEnumerable<SingleAlleleScoringInfo> alleleScoringInfos, 
            IEnumerable<SerologyEntry> matchingSerologies)
        {
            AlleleScoringInfos = alleleScoringInfos;
            MatchingSerologies = matchingSerologies;
        }

        public static MultipleAlleleScoringInfo GetScoringInfo(
            IEnumerable<IHlaLookupResultSource<AlleleTyping>> lookupResultSources)
        {
            var sources = lookupResultSources.ToList();

            var alleleScoringInfos = sources
                .Select(SingleAlleleScoringInfo.GetScoringInfoExcludingMatchingSerologies);

            var matchingSerologies = sources
                .SelectMany(source => source.MatchingSerologies)
                .Select(matchingSerology => matchingSerology.ToSerologyEntry())
                .Distinct();

            return new MultipleAlleleScoringInfo(
                alleleScoringInfos,
                matchingSerologies);
        }

        public List<SingleAlleleScoringInfo> ConvertToSingleAllelesInfo() => AlleleScoringInfos.ToList();

        #region IEquatable
        public bool Equals(MultipleAlleleScoringInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                AlleleScoringInfos.SequenceEqual(other.AlleleScoringInfos) && 
                MatchingSerologies.SequenceEqual(other.MatchingSerologies);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MultipleAlleleScoringInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (AlleleScoringInfos.GetHashCode() * 397) ^ MatchingSerologies.GetHashCode();
            }
        }
        #endregion
    }
}
