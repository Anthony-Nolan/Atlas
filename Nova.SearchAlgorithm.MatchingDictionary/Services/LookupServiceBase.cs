using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public abstract class LookupServiceBase<T>
    {
        public async Task<T> GetLookupResults(MatchLocus matchLocus, string lookupName)
        {
            try
            {
                if (!LookupNameIsValid(lookupName))
                {
                    throw new ArgumentException($"{lookupName} at locus {matchLocus} is not a valid lookup name.");
                }

                var formattedLookupName = FormatLookupName(lookupName);

                return await PerformLookup(matchLocus, formattedLookupName);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to lookup \"{lookupName}\" at locus {matchLocus}.";
                throw new MatchingDictionaryException(msg, ex);
            }
        }

        protected abstract bool LookupNameIsValid(string lookupName);

        protected abstract Task<T> PerformLookup(MatchLocus matchLocus, string lookupName);

        private static string FormatLookupName(string lookupName)
        {
            return lookupName.Trim().TrimStart('*');
        }
    }
}