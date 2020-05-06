using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups
{
    /// <summary>
    /// Data returned from a HLA lookup.
    /// </summary>
    public interface IHlaLookupResult : IStorableInCloudTable<HlaLookupTableEntity>
    {
        Locus Locus { get; }

        /// <summary>
        /// LookupName refers to the HLA name as stored in the lookup repository.
        /// It may differ to the submitted HLA name.
        /// </summary>
        string LookupName { get; }

        TypingMethod TypingMethod { get; }

        /// <summary>
        /// Property containing HLA information to be stored as a serialised string.
        /// </summary>
        object HlaInfoToSerialise { get; }
    }
}
