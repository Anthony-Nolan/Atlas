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
        protected Dictionary<Locus, Dictionary<string, TSerialisableHlaMetadata>> HlaMetadata;

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

        public async Task<HlaMetadataTableRow> GetHlaMetadataRowIfExists(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaNomenclatureVersion)
        {
            var metadata = await LookupMetadata(locus, lookupName);
            return metadata == null ? null : new HlaMetadataTableRow(metadata);
        }

        protected Task<TSerialisableHlaMetadata> LookupMetadata(Locus locus, string lookupName)
        {
            var locusDictionary = HlaMetadata[locus];
            var metadata = locusDictionary.GetValueOrDefault(lookupName);
            return Task.FromResult(metadata);
        }

        private void PopulateHlaMetadata()
        {
            var metadataCollection = GetMetadataFromJsonFile();
            var allMetadata = GetHlaMetadata(metadataCollection);

            HlaMetadata = allMetadata
                .GroupBy(m => m.Locus)
                .ToDictionary(
                    locusGroup => locusGroup.Key,
                    locusGroup => locusGroup.ToDictionary(
                        metadata => metadata.LookupName,
                        metadata => metadata
                    )
                );
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
            if (loadedFile != null)
            {
                return loadedFile;
            }

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