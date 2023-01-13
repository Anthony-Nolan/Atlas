using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
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
        // Dictionary of dictionaries. First layer keyed by HlaVersion. Second layer by (locus, lookupName).
        // Make no effort to protect against innappropriate versions being passed in.
        protected Dictionary<string, Dictionary<(Locus, string), TSerialisableHlaMetadata>> HlaMetadata = new Dictionary<string, Dictionary<(Locus, string), TSerialisableHlaMetadata>>();

        protected FileBackedHlaMetadataRepositoryBase()
        {
            PopulateHlaMetadata(OlderTestHlaVersion);
            PopulateHlaMetadata(NewerTestsHlaVersion);
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
            var metadata = await LookupMetadata(locus, lookupName, hlaNomenclatureVersion);
            return metadata == null ? null : new HlaMetadataTableRow(metadata);
        }

        protected Task<TSerialisableHlaMetadata> LookupMetadata(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            var metadata = HlaMetadata[hlaNomenclatureVersion].GetValueOrDefault((locus, lookupName));
            return Task.FromResult(metadata);
        }

        // This method has a one-off cost of ~2s. Therefore the first test run in a suite using the file-backed dictionary will have a ~2s delay.
        private void PopulateHlaMetadata(string hlaNomenclatureVersion)
        {
            var metadataCollection = GetMetadataFromJsonFile(hlaNomenclatureVersion);
            var allMetadata = GetHlaMetadata(metadataCollection);

            HlaMetadata[hlaNomenclatureVersion] = allMetadata
                .ToDictionary(
                    m => (m.Locus, m.LookupName),
                    m => m
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
        public const string OlderTestHlaVersion = "3330";
        public const string NewerTestsHlaVersion = "3400";

        private static readonly Dictionary<string, FileBackedHlaMetadataCollection> PreviouslyLoadedFiles = new Dictionary<string, FileBackedHlaMetadataCollection>();

        protected static FileBackedHlaMetadataCollection GetMetadataFromJsonFile(string hlaNomenclatureVersion)
        {
            if (PreviouslyLoadedFiles.TryGetValue(hlaNomenclatureVersion, out var loadedFile))
            {
                return loadedFile;
            }

            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream($"Atlas.HlaMetadataDictionary.Test.IntegrationTests.Resources.all_hla_metadata_v{hlaNomenclatureVersion}.json"))
            using (var reader = new StreamReader(stream))
            {
                loadedFile = JsonConvert.DeserializeObject<FileBackedHlaMetadataCollection>(reader.ReadToEnd());
            }

            var forceEvaluation = loadedFile.AlleleNameMetadata.Count(); //Forces population of all the IEnumerable Properties.
            PreviouslyLoadedFiles[hlaNomenclatureVersion] = loadedFile;
            return loadedFile;
        }
    }
}