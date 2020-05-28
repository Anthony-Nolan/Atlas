using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;

namespace Atlas.HlaMetadataDictionary.Models.Lookups.Dpb1TceGroupLookup
{
    public interface IDpb1TceGroupsLookupResult : ISerialisableHlaMetadata
    {
        string TceGroup { get; }
    }

    internal class Dpb1TceGroupsLookupResult : IDpb1TceGroupsLookupResult
    {
        public Locus Locus => Locus.Dpb1;
        public string LookupName { get; }
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public string TceGroup { get; }
        public object HlaInfoToSerialise => TceGroup;

        public Dpb1TceGroupsLookupResult(
            string lookupName,
            string tceGroup)
        {
            LookupName = lookupName;
            TceGroup = tceGroup;
        }
    }
}
