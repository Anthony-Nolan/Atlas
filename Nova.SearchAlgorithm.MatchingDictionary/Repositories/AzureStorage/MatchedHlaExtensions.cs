using Newtonsoft.Json;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class MatchedHlaExtensions
    {
        internal static MatchedHlaTableEntity ToTableEntity(this IMatchedHla matchedHla)
        {
            return new MatchedHlaTableEntity(matchedHla.HlaType.MatchLocus, matchedHla.HlaType.Name)
            {
                SerialisedMatchedHla = JsonConvert.SerializeObject(matchedHla)
            };
        }

        internal static IMatchedHla ToMatchedHla(this MatchedHlaTableEntity result)
        {
            return JsonConvert.DeserializeObject<IMatchedHla>(result.SerialisedMatchedHla);
        }
    }
}