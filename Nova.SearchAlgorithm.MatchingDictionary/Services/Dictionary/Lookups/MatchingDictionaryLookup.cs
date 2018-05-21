using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary.Lookups
{
    /// <summary>
    /// This class is responsible for
    /// providing base functionality 
    /// for the different types of dictionary lookup.
    /// </summary>
    internal abstract class MatchingDictionaryLookup
    {
        private readonly IMatchedHlaRepository dictionaryRepository;

        protected MatchingDictionaryLookup(IMatchedHlaRepository dictionaryRepository)
        {
            this.dictionaryRepository = dictionaryRepository;
        }

        public abstract Task<MatchingDictionaryEntry> PerformLookupAsync(MatchLocus matchLocus, string lookupName);

        protected async Task<MatchingDictionaryEntry> GetDictionaryEntry(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var entry = await dictionaryRepository.GetDictionaryEntry(matchLocus, lookupName, typingMethod);
            return entry ?? throw new InvalidHlaException(matchLocus.ToString(), lookupName);
        }
    }
}
