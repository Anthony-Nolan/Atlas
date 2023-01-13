using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata
{
    /// <summary>
    /// HLA Metadata. i.e. something with the key necessary to be looked up in our HLA Metadata Dictionary.
    /// </summary>
    public interface IHlaMetadata
    {
        Locus Locus { get; }

        /// <summary>
        /// LookupName refers to the HLA name as stored in the lookup repository.
        /// It may differ to the submitted HLA name.
        /// </summary>
        string LookupName { get; }

        TypingMethod TypingMethod { get; }
    }
    
    public interface ISerialisableHlaMetadata : IHlaMetadata
    {
        /// <summary>
        /// Property containing HLA information to be stored as a serialised string.
        /// </summary>
        object HlaInfoToSerialise { get; }

        string SerialisedHlaInfoType { get; }
    }
}
