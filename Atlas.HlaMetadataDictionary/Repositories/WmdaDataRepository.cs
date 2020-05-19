using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.Data;
using Atlas.HlaMetadataDictionary.Models.Wmda;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.WmdaExtractors;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.WmdaExtractors.AlleleGroupExtractors;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.WmdaExtractors.HlaNomExtractors;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.WmdaExtractors.SerologyRelationshipExtractors;

namespace Atlas.HlaMetadataDictionary.Repositories
{
    public interface IWmdaDataRepository
    {
        WmdaDataset GetWmdaDataset(string hlaDatabaseVersion);
    }

    public class WmdaDataRepository : IWmdaDataRepository
    {
        private readonly Dictionary<string, WmdaDataset> wmdaDatasets = new Dictionary<string, WmdaDataset>();

        private readonly IWmdaFileReader wmdaFileReader;

        public WmdaDataRepository(IWmdaFileReader wmdaFileReader)
        {
            this.wmdaFileReader = wmdaFileReader;
        }

        public WmdaDataset GetWmdaDataset(string hlaDatabaseVersion)
        {
            if (!wmdaDatasets.TryGetValue(hlaDatabaseVersion, out var dataset))
            {
                dataset = FetchWmdaDataCollections(hlaDatabaseVersion);
                wmdaDatasets.Add(hlaDatabaseVersion, dataset);
            }

            return dataset;
        }

        private WmdaDataset FetchWmdaDataCollections(string hlaDatabaseVersion)
        {
            return new WmdaDataset
            {
                HlaDatabaseVersion = hlaDatabaseVersion,
                Serologies = GetWmdaData(new SerologyExtractor(), hlaDatabaseVersion).ToList(),
                Alleles = GetWmdaData(new AlleleExtractor(), hlaDatabaseVersion).ToList(),
                PGroups = GetWmdaData(new PGroupExtractor(), hlaDatabaseVersion).ToList(),
                GGroups = GetWmdaData(new GGroupExtractor(), hlaDatabaseVersion).ToList(),
                SerologyToSerologyRelationships = GetWmdaData(new SerologyToSerologyRelationshipExtractor(), hlaDatabaseVersion).ToList(),
                AlleleToSerologyRelationships = GetWmdaData(new AlleleToSerologyRelationshipExtractor(), hlaDatabaseVersion).ToList(),
                ConfidentialAlleles = GetWmdaData(new ConfidentialAlleleExtractor(), hlaDatabaseVersion).ToList(),
                AlleleStatuses = GetWmdaData(new AlleleStatusExtractor(), hlaDatabaseVersion).ToList(),
                AlleleNameHistories = GetWmdaData(new AlleleHistoryExtractor(), hlaDatabaseVersion).ToList(),
                Dpb1TceGroupAssignments = GetWmdaData(new Dpb1TceGroupAssignmentExtractor(), hlaDatabaseVersion).ToList(),
            };
        }

        private IEnumerable<TWmdaHlaTyping> GetWmdaData<TWmdaHlaTyping>(WmdaDataExtractor<TWmdaHlaTyping> extractor, string hlaDatabaseVersion)
            where TWmdaHlaTyping : IWmdaHlaTyping
        {
            return extractor.GetWmdaHlaTypingsForHlaMetadataDictionaryLoci(wmdaFileReader, hlaDatabaseVersion);
        }
    }
}