using Atlas.Common.GeneticData.Hla.Models;

namespace Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup
{
    /// <summary>
    /// Lookup result with data required to score HLA pairings.
    /// </summary>
    public interface IHlaScoringLookupResult : IHlaLookupResult
    {
        HlaTypingCategory HlaTypingCategory { get; }
        IHlaScoringInfo HlaScoringInfo { get; }
    }
}
