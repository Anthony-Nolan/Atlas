using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using System;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public abstract class LookupServiceBase<T>
    {
        protected async Task<T> GetLookupResults(Locus locus, string lookupName, string hlaDatabaseVersion)
        {
            try
            {
                if (!LookupNameIsValid(lookupName))
                {
                    throw new ArgumentException($"{lookupName} at locus {locus} is not a valid lookup name.");
                }

                var formattedLookupName = FormatLookupName(lookupName);

                return await PerformLookup(locus, formattedLookupName, hlaDatabaseVersion);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to lookup '{lookupName}' at locus {locus}.";
                throw new MatchingDictionaryException(msg, ex);
            }
        }

        protected abstract bool LookupNameIsValid(string lookupName);

        protected abstract Task<T> PerformLookup(Locus locus, string lookupName, string hlaDatabaseVersion);

        private static string FormatLookupName(string lookupName)
        {
            return lookupName.Trim().TrimStart('*');
        }
    }
}