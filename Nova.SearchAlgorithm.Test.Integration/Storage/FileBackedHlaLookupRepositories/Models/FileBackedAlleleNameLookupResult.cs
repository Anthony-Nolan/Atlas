using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories.Models
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