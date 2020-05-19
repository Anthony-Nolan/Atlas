using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;

namespace Atlas.HlaMetadataDictionary.Models.Lookups
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
