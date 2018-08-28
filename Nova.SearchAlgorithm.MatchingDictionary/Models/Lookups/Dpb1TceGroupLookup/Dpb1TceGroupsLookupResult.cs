using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.Dpb1TceGroupLookup
{
    public interface IDpb1TceGroupsLookupResult : IHlaLookupResult
    {
        IEnumerable<string> TceGroups { get; }
    }

    public class Dpb1TceGroupsLookupResult : IDpb1TceGroupsLookupResult
    {
        public MatchLocus MatchLocus => MatchLocus.Dpb1;
        public string LookupName { get; }
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public IEnumerable<string> TceGroups { get; }
        public object HlaInfoToSerialise => TceGroups;

        public Dpb1TceGroupsLookupResult(
            string lookupName,
            IEnumerable<string> tceGroups)
        {
            LookupName = lookupName;
            TceGroups = tceGroups;
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return new HlaLookupTableEntity(this);
        }
    }
}
