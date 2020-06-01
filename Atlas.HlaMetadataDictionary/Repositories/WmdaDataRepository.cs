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
    internal interface IWmdaDataRepository
    {
        WmdaDataset GetWmdaDataset(string hlaNomenclatureVersion);
    }

    internal class WmdaDataRepository : IWmdaDataRepository
    {
        private readonly Dictionary<string, WmdaDataset> wmdaDatasets = new Dictionary<string, WmdaDataset>();

        private readonly IWmdaFileReader wmdaFileReader;

        public WmdaDataRepository(IWmdaFileReader wmdaFileReader)
        {
            this.wmdaFileReader = wmdaFileReader;
        }

        public WmdaDataset GetWmdaDataset(string hlaNomenclatureVersion)
        {
            if (!wmdaDatasets.TryGetValue(hlaNomenclatureVersion, out var dataset))
            {
                dataset = FetchWmdaDataCollections(hlaNomenclatureVersion);
                wmdaDatasets.Add(hlaNomenclatureVersion, dataset);
            }

            return dataset;
        }

        private WmdaDataset FetchWmdaDataCollections(string hlaNomenclatureVersion)
        {
            return new WmdaDataset
            {
                HlaNomenclatureVersion = hlaNomenclatureVersion,
                Serologies = GetWmdaData(new SerologyExtractor(), hlaNomenclatureVersion).ToList(),
                Alleles = GetWmdaData(new AlleleExtractor(), hlaNomenclatureVersion).ToList(),
                PGroups = GetWmdaData(new PGroupExtractor(), hlaNomenclatureVersion).ToList(),
                GGroups = GetWmdaData(new GGroupExtractor(), hlaNomenclatureVersion).ToList(),
                SerologyToSerologyRelationships = GetWmdaData(new SerologyToSerologyRelationshipExtractor(), hlaNomenclatureVersion).ToList(),
                AlleleToSerologyRelationships = GetWmdaData(new AlleleToSerologyRelationshipExtractor(), hlaNomenclatureVersion).ToList(),
                ConfidentialAlleles = GetWmdaData(new ConfidentialAlleleExtractor(), hlaNomenclatureVersion).ToList(),
                AlleleStatuses = GetWmdaData(new AlleleStatusExtractor(), hlaNomenclatureVersion).ToList(),
                AlleleNameHistories = GetWmdaData(new AlleleHistoryExtractor(), hlaNomenclatureVersion).ToList(),
                Dpb1TceGroupAssignments = GetWmdaData(new Dpb1TceGroupAssignmentExtractor(), hlaNomenclatureVersion).ToList(),
            };
        }

        private IEnumerable<TWmdaHlaTyping> GetWmdaData<TWmdaHlaTyping>(WmdaDataExtractor<TWmdaHlaTyping> extractor, string hlaNomenclatureVersion)
            where TWmdaHlaTyping : IWmdaHlaTyping
        {
            return extractor.GetWmdaHlaTypingsForHlaMetadataDictionaryLoci(wmdaFileReader, hlaNomenclatureVersion);
        }
    }
}