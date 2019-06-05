using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups
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
