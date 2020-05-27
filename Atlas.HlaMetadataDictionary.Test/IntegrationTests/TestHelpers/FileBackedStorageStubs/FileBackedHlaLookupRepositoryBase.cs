using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;
using Newtonsoft.Json;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs
{
    /// <summary>
    /// An implementation of a HLA lookup repository which loads the data from a file,
    /// necessary for testing without an internet dependency.
    /// </summary>
    internal abstract class FileBackedHlaLookupRepositoryBase<THlaLookupResult> :
        FileBackedHlaLookupRepositoryBaseReader,
        IHlaLookupRepository
        where THlaLookupResult : ISerialisableHlaMetadata
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

        public Task RecreateHlaLookupTable(IEnumerable<ISerialisableHlaMetadata> lookupResults, string hlaDatabaseVersion)
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

            var entity = lookupResult == null ? null : new HlaLookupTableEntity(lookupResult);
            return Task.FromResult(entity);
        }

        private void PopulateHlaLookupResults()
        {
            var resultCollections = GetLookupResultsFromJsonFile();
            HlaLookupResults = GetHlaLookupResults(resultCollections);
        }

        protected abstract IEnumerable<THlaLookupResult> GetHlaLookupResults(FileBackedHlaLookupResultCollections resultCollections);
    }

    /// <summary>
    /// Static variables exist per-type for generic classes.
    /// So this would read afresh for each distinct class, if it lived in the class above.
    /// Having this class allows us to read the file only once.
    /// </summary>
    public abstract class FileBackedHlaLookupRepositoryBaseReader
    {
        private static FileBackedHlaLookupResultCollections loadedFile = null;
        protected static FileBackedHlaLookupResultCollections GetLookupResultsFromJsonFile()
        {
            if (loadedFile != null) { return loadedFile; }

            var assem = Assembly.GetExecutingAssembly();
            using (var stream = assem.GetManifestResourceStream("Atlas.HlaMetadataDictionary.Test.IntegrationTests.Resources.all_hla_lookup_results.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                    loadedFile = JsonConvert.DeserializeObject<FileBackedHlaLookupResultCollections>(reader.ReadToEnd());
                }
            }

            var forceEvaluation = loadedFile.AlleleNameLookupResults.Count(); //Forces population of all the IEnumerables.
            return loadedFile;
        }
    }
}