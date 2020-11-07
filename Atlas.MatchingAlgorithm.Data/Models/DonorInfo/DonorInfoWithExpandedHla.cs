using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;

namespace Atlas.MatchingAlgorithm.Data.Models.DonorInfo
{
    public class DonorInfoWithExpandedHla : DonorInfo
    {
        public PhenotypeInfo<IHlaMatchingMetadata> MatchingHla { get; set; }

        internal IEnumerable<string> FlatHlaNames => HlaNames.ToEnumerable();
    }

    public static class DonorInfoExtensions
    {
        public static IList<string> AllHlaNames(this IEnumerable<DonorInfoWithExpandedHla> donorInfos)
        {
            return donorInfos.SelectMany(d => d.FlatHlaNames).Distinct().ToList();
        }

        public static IList<string> AllPGroupNames(this IEnumerable<DonorInfoWithExpandedHla> donorInfos)
        {
            return donorInfos.SelectMany(d => d.MatchingHla.ToEnumerable().SelectMany(p => p?.MatchingPGroups ?? new List<string>())).ToList();
        }

        public static DonorInfoForHlaPreProcessing ToDonorInfoForPreProcessing(
            this DonorInfoWithExpandedHla donorInfoWithExpandedHla,
            Func<string, int> lookupHlaNameId)
        {
            return new DonorInfoForHlaPreProcessing
            {
                DonorId = donorInfoWithExpandedHla.DonorId,
                HlaNameIds = donorInfoWithExpandedHla.HlaNames.Map(hlaName => hlaName == null ? null as int? : lookupHlaNameId(hlaName))
            };
        }
    }
}