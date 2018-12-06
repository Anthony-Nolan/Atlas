using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories
{
    /// <summary>
    /// An implementation of a HLA lookup repository which loads the data from a file,
    /// necessary for testing without an internet dependency.
    /// </summary>
    public abstract class FileBackedHlaLookupRepositoryBase<THlaLookupResult> :
        IHlaLookupRepository
        where THlaLookupResult : IHlaLookupResult
    {
        protected IEnumerable<THlaLookupResult> HlaLookupResults;

        protected FileBackedHlaLookupRepositoryBase()
        {
            PopulateHlaLookupResults();
        }

        public Task RecreateDataTable(IEnumerable<IHlaLookupResult> tableContents, IEnumerable<string> partitions)
        {
            // No operation needed
            return Task.CompletedTask;
        }

        public Task LoadDataIntoMemory()
        {
            // No operation needed
            return Task.CompletedTask;
        }

        public Task RecreateHlaLookupTable(IEnumerable<IHlaLookupResult> lookupResults)
        {
            // No operation needed
            return Task.CompletedTask;
        }

        public Task<HlaLookupTableEntity> GetHlaLookupTableEntityIfExists(Locus locus, string lookupName, TypingMethod typingMethod)
        {
            var lookupResult = HlaLookupResults.FirstOrDefault(hla => 
                hla.Locus.Equals(locus) && hla.LookupName == lookupName);

            return Task.FromResult(lookupResult?.ConvertToTableEntity());
        }

        private void PopulateHlaLookupResults()
        {
            var resultCollections = GetLookupResultsFromJsonFile();
            HlaLookupResults = GetHlaLookupResults(resultCollections);
        }

        private static FileBackedHlaLookupResultCollections GetLookupResultsFromJsonFile()
        {
            var assem = System.Reflection.Assembly.GetExecutingAssembly();
            using (var stream = assem.GetManifestResourceStream("Nova.SearchAlgorithm.Test.Integration.Resources.MatchingDictionary.all_hla_lookup_results.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                    return JsonConvert.DeserializeObject<FileBackedHlaLookupResultCollections>(reader.ReadToEnd());
                }
            }
        }

        protected abstract IEnumerable<THlaLookupResult> GetHlaLookupResults(FileBackedHlaLookupResultCollections resultCollections);
    }
}
