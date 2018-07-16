using Newtonsoft.Json;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary.ScoringLookup;
using System;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class HlaScoringLookupResultExtensions
    {
        internal static HlaLookupTableEntity ToTableEntity(this IHlaScoringLookupResult lookupResult)
        {
            return new HlaLookupTableEntity(lookupResult.MatchLocus, lookupResult.LookupName, lookupResult.TypingMethod)
            {
                HlaTypingCategory = lookupResult.HlaTypingCategory,
                SerialisedHlaInfo = JsonConvert.SerializeObject(lookupResult.PreCalculatedHlaInfo)
            };
        }

        internal static IHlaScoringLookupResult ToHlaScoringLookupResult(this HlaLookupTableEntity entity)
        {
            var scoringInfo = GetPreCalculatedScoringInfo(entity);

            return new HlaScoringLookupResult(
                entity.MatchLocus,
                entity.LookupName,
                entity.TypingMethod,
                entity.HlaTypingCategory,
                scoringInfo);
        }

        private static IPreCalculatedScoringInfo GetPreCalculatedScoringInfo(HlaLookupTableEntity entity)
        {
            IPreCalculatedScoringInfo scoringInfo;

            switch (entity.HlaTypingCategory)
            {
                case HlaTypingCategory.Allele:
                    scoringInfo = GetScoringInfo<SingleAlleleScoringInfo>(entity);
                    break;
                case HlaTypingCategory.Serology:
                    scoringInfo = GetScoringInfo<SerologyScoringInfo>(entity);
                    break;
                case HlaTypingCategory.XxCode:
                    scoringInfo = GetScoringInfo<XxCodeScoringInfo>(entity);
                    break;
                case HlaTypingCategory.AlleleStringOfNames:
                case HlaTypingCategory.AlleleStringOfSubtypes:
                case HlaTypingCategory.NmdpCode:
                    scoringInfo = GetScoringInfo<AlleleStringScoringInfo>(entity);
                    break;
                default:
                    throw new NotImplementedException();
            }
            return scoringInfo;
        }

        private static IPreCalculatedScoringInfo GetScoringInfo<TScoringInfo>(HlaLookupTableEntity entity)
            where TScoringInfo : IPreCalculatedScoringInfo
        {
            return JsonConvert.DeserializeObject<TScoringInfo>(entity.SerialisedHlaInfo);
        }
    }
}