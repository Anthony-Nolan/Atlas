using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Extensions;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata
{
    internal abstract class SerialisableHlaMetadata : ISerialisableHlaMetadata
    {
        public Locus Locus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public abstract object HlaInfoToSerialise { get; }
        public string SerialisedHlaInfoType => HlaInfoToSerialise?.GetType().GetNeatCSharpName();

        protected SerialisableHlaMetadata(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod)
        {
            Locus = locus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
        }
    }
}
