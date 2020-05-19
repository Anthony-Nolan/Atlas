using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories
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

        public Task RecreateDataTable(IEnumerable<IHlaLookupResult> tableContents, IEnumerable<string> partitions, string hlaDatabaseVersion)
        {
            // No operation needed
            return Task.CompletedTask;
        }

        public Task LoadDataIntoMemory(string hlaDatabaseVersion)
        {
            // No operation needed
            return Task.CompletedTask;
        }

        public Task RecreateHlaLookupTable(IEnumerable<IHlaLookupResult> lookupResults, string hlaDatabaseVersion)
        {
            // No operation needed
            return Task.CompletedTask;
        }

        public Task<HlaLookupTableEntity> GetHlaLookupTableEntityIfExists(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaDatabaseVersion)
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
            var assem = Assembly.GetExecutingAssembly();
            using (var stream =
                assem.GetManifestResourceStream("Atlas.MatchingAlgorithm.Test.Integration.Resources.HlaMetadataDictionary.all_hla_lookup_results.json"))
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