using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups.AlleleNameLookup;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using Atlas.Common.GeneticData;

namespace Atlas.MatchingAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories.Models
{
    public class FileBackedAlleleNameLookupResult : IAlleleNameLookupResult
    {
        public Locus Locus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public object HlaInfoToSerialise { get; }
        public IEnumerable<string> CurrentAlleleNames { get; }

        public FileBackedAlleleNameLookupResult(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            IEnumerable<string> currentAlleleNames)
        {
            Locus = locus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            HlaInfoToSerialise = currentAlleleNames;
            CurrentAlleleNames = currentAlleleNames;
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return new HlaLookupTableEntity(this);
        }
    }
}