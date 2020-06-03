using System;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Exceptions;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    internal abstract class MetadataServiceBase<T>
    {
        protected async Task<T> GetMetadata(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            try
            {
                if (!LookupNameIsValid(lookupName))
                {
                    throw new ArgumentException($"{lookupName} at locus {locus} is not a valid lookup name.");
                }

                var formattedLookupName = FormatLookupName(lookupName);

                return await PerformLookup(locus, formattedLookupName, hlaNomenclatureVersion);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to lookup '{lookupName}' at locus {locus}.";
                throw new HlaMetadataDictionaryException(locus, lookupName, msg, ex);
            }
        }

        protected abstract bool LookupNameIsValid(string lookupName);

        protected abstract Task<T> PerformLookup(Locus locus, string lookupName, string hlaNomenclatureVersion);

        private static string FormatLookupName(string lookupName)
        {
            return lookupName.Trim().TrimStart('*');
        }
    }
}