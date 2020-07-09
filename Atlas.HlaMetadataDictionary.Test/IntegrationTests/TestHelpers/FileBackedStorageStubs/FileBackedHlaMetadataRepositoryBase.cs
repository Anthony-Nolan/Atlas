using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Newtonsoft.Json;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs
{
    /// <summary>
    /// An implementation of a HLA lookup repository which loads the data from a file,
    /// necessary for testing without an internet dependency.
    /// </summary>
    internal abstract class FileBackedHlaMetadataRepositoryBase<TSerialisableHlaMetadata> :
        FileBackedHlaMetadataRepositoryBaseReader,
        IHlaMetadataRepository
        where TSerialisableHlaMetadata : ISerialisableHlaMetadata
    {
        protected IEnumerable<TSerialisableHlaMetadata> HlaMetadata;

        protected FileBackedHlaMetadataRepositoryBase()
        {
            PopulateHlaMetadata();
        }

        public Task LoadDataIntoMemory(string hlaNomenclatureVersion)
        {
            // No operation needed
            return Task.CompletedTask;
        }

        public Task RecreateHlaMetadataTable(IEnumerable<ISerialisableHlaMetadata> metadata, string hlaNomenclatureVersion)
        {
            // No operation needed
            return Task.CompletedTask;
        }

        public Task<HlaMetadataTableRow> GetHlaMetadataRowIfExists(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaNomenclatureVersion)
        {
            var metadata = HlaMetadata.FirstOrDefault(hla =>
                hla.Locus.Equals(locus) && hla.LookupName == lookupName);

            var entity = metadata == null ? null : new HlaMetadataTableRow(metadata);
            return Task.FromResult(entity);
        }

        // This method has a one-off cost of ~2s. Therefore the first test run in a suite using the file-backed dictionary will have a ~2s delay.
        private void PopulateHlaMetadata()
        {
            var metadataCollection = GetMetadataFromJsonFile();
            HlaMetadata = GetHlaMetadata(metadataCollection);
        }

        protected abstract IEnumerable<TSerialisableHlaMetadata> GetHlaMetadata(FileBackedHlaMetadataCollection metadataCollection);
    }

    /// <summary>
    /// Static variables exist per-type for generic classes.
    /// So this would read afresh for each distinct class, if it lived in the class above.
    /// Having this class allows us to read the file only once.
    /// </summary>
    public abstract class FileBackedHlaMetadataRepositoryBaseReader
    {
        private static FileBackedHlaMetadataCollection loadedFile = null;
        protected static FileBackedHlaMetadataCollection GetMetadataFromJsonFile()
        {
            if (loadedFile != null) { return loadedFile; }

            var assem = Assembly.GetExecutingAssembly();
            using (var stream = assem.GetManifestResourceStream("Atlas.HlaMetadataDictionary.Test.IntegrationTests.Resources.all_hla_metadata.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                    loadedFile = JsonConvert.DeserializeObject<FileBackedHlaMetadataCollection>(reader.ReadToEnd());
                }
            }

            var forceEvaluation = loadedFile.AlleleNameMetadata.Count(); //Forces population of all the IEnumerables.
            return loadedFile;
        }
    }
}