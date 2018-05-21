using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary.Lookups
{
    /// <summary>
    /// This class is responsible for
    /// looking up a single allele typing in the matching dictionary.
    /// </summary>
    internal class AlleleLookup : MatchingDictionaryLookup
    {
        public AlleleLookup(IMatchedHlaRepository dictionaryRepository) : base(dictionaryRepository)
        {
        }

        public override Task<MatchingDictionaryEntry> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            var allele = new Allele(LocusNames.GetMolecularLocusNameFromMatch(matchLocus), lookupName);
            return GetDictionaryEntry(matchLocus, allele.TwoFieldName, TypingMethod.Molecular);
        }
    }
}
