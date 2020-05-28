using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;

namespace Atlas.HlaMetadataDictionary.Models.Lookups
{
    /// <summary>
    /// Data returned from a HLA lookup.
    /// </summary>
    public interface IHlaLookupResult
    {
        Locus Locus { get; }

        /// <summary>
        /// LookupName refers to the HLA name as stored in the lookup repository.
        /// It may differ to the submitted HLA name.
        /// </summary>
        string LookupName { get; }

        TypingMethod TypingMethod { get; }
    }


    public interface ISerialisableHlaMetadata : IHlaLookupResult
    {
        /// <summary>
        /// Property containing HLA information to be stored as a serialised string.
        /// </summary>
        object HlaInfoToSerialise { get; }
    }
}
