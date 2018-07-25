using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups
{
    public static class MatchingSerologyExtensions
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
