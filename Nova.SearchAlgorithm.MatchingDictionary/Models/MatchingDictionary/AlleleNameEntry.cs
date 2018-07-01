using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary
{
    public class AlleleNameEntry
    {
        public MatchLocus MatchLocus { get; }
        public string LookupName { get; }
        public string CurrentAlleleName { get; }

        public AlleleNameEntry(MatchLocus matchLocus, string lookupName, string currentAlleleName)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            CurrentAlleleName = currentAlleleName;
        }

        public AlleleNameEntry(string locus, string lookupName, string currentAlleleName) 
            : this(
                  PermittedLocusNames.GetMatchLocusNameFromTypingLocusIfExists(TypingMethod.Molecular, locus),
                  lookupName, 
                  currentAlleleName)
        {
        }
    }
}
