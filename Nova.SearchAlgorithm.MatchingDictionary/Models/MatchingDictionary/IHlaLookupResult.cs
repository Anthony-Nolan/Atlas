using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary
{
    /// <summary>
    /// Data returned from a HLA lookup.
    /// </summary>
    public interface IHlaLookupResult
    {
        MatchLocus MatchLocus { get; }

        /// <summary>
        /// LookupName refers to the HLA name as stored in the lookup repository.
        /// It may differ to the submitted HLA name.
        /// </summary>
        string LookupName { get; }

        TypingMethod TypingMethod { get; }
    }
}
