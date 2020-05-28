using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.Lookups.Dpb1TceGroupLookup;

namespace Atlas.MatchingAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories.Models
{
    public class FileBackedDpb1TceGroupsLookupResult : IDpb1TceGroupsLookupResult
    {
        public Locus Locus => Locus.Dpb1;
        public string LookupName { get; }
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public string TceGroup { get; }
        public object HlaInfoToSerialise => TceGroup;

        public FileBackedDpb1TceGroupsLookupResult(string lookupName, string tceGroup)
        {
            LookupName = lookupName;
            TceGroup = tceGroup;
        }
    }
}