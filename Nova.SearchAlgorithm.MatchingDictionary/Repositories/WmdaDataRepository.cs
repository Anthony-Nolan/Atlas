using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IWmdaDataRepository
    {
        IEnumerable<HlaNom> Serologies { get; }
        IEnumerable<HlaNom> Alleles { get; }
        IEnumerable<HlaNomP> PGroups { get; }
        IEnumerable<HlaNomG> GGroups { get; }
        IEnumerable<RelSerSer> SerologyToSerologyRelationships { get; }
        IEnumerable<RelDnaSer> AlleleToSerologyRelationships { get; }
        IEnumerable<ConfidentialAllele> ConfidentialAlleles { get; }
        IEnumerable<AlleleStatus> AlleleStatuses { get; }
    }

    public class WmdaDataRepository : IWmdaDataRepository
    {
        public IEnumerable<HlaNom> Serologies { get; private set; }
        public IEnumerable<HlaNom> Alleles { get; private set; }
        public IEnumerable<HlaNomP> PGroups { get; private set; }
        public IEnumerable<HlaNomG> GGroups { get; private set; }
        public IEnumerable<RelSerSer> SerologyToSerologyRelationships { get; private set; }
        public IEnumerable<RelDnaSer> AlleleToSerologyRelationships { get; private set; }
        public IEnumerable<ConfidentialAllele> ConfidentialAlleles { get; private set; }
        public IEnumerable<AlleleStatus> AlleleStatuses { get; private set; }

        private readonly IWmdaFileReader wmdaFileReader;

        public WmdaDataRepository(IWmdaFileReader wmdaFileReader)
        {
            this.wmdaFileReader = wmdaFileReader;
            PopulateWmdaDataCollections();
        }

        private void PopulateWmdaDataCollections()
        {
            Serologies = GetWmdaData(new SerologyExtractor());
            Alleles = GetWmdaData(new AlleleExtractor());
            PGroups = GetWmdaData(new PGroupExtractor());
            GGroups = GetWmdaData(new GGroupExtractor());
            SerologyToSerologyRelationships = GetWmdaData(new SerologyToSerologyRelationshipExtractor());
            AlleleToSerologyRelationships = GetWmdaData(new AlleleToSerologyRelationshipExtractor());
            ConfidentialAlleles = GetWmdaData(new ConfidentialAlleleExtractor());
            AlleleStatuses = GetWmdaData(new AlleleStatusExtractor());
        }

        private IEnumerable<TWmdaHlaTyping> GetWmdaData<TWmdaHlaTyping>(WmdaDataExtractor<TWmdaHlaTyping> extractor)
            where TWmdaHlaTyping : IWmdaHlaTyping
        {
            return extractor.GetWmdaHlaTypingsForPermittedLoci(wmdaFileReader);
        }
    }
}
