using Atlas.MatchingAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups
{
    internal static class MatchingSerologyExtensions
    {
        public static SerologyEntry ToSerologyEntry(this MatchingSerology matchingSerology)
        {
            return new SerologyEntry(
                matchingSerology.SerologyTyping.Name,
                matchingSerology.SerologyTyping.SerologySubtype,
                matchingSerology.IsDirectMapping);
        }
    }
}
