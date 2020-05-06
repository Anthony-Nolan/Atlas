using Atlas.HlaMetadataDictionary.Models.MatchingTypings;

namespace Atlas.HlaMetadataDictionary.Models.Lookups
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
