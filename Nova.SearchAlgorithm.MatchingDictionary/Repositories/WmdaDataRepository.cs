using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IWmdaDataRepository
    {
        string HlaDatabaseVersion { get; }
        IEnumerable<HlaNom> Serologies { get; }
        IEnumerable<HlaNom> Alleles { get; }
        IEnumerable<HlaNomP> PGroups { get; }
        IEnumerable<HlaNomG> GGroups { get; }
        IEnumerable<RelSerSer> SerologyToSerologyRelationships { get; }
        IEnumerable<RelDnaSer> AlleleToSerologyRelationships { get; }
        IEnumerable<ConfidentialAllele> ConfidentialAlleles { get; }
        IEnumerable<AlleleStatus> AlleleStatuses { get; }
        IEnumerable<AlleleNameHistory> AlleleNameHistories { get; }
        IEnumerable<Dpb1TceGroup> Dpb1TceGroups { get; }
    }

    public class WmdaDataRepository : IWmdaDataRepository
    {
        public string HlaDatabaseVersion { get; }
        public IEnumerable<HlaNom> Serologies { get; private set; }
        public IEnumerable<HlaNom> Alleles { get; private set; }
        public IEnumerable<HlaNomP> PGroups { get; private set; }
        public IEnumerable<HlaNomG> GGroups { get; private set; }
        public IEnumerable<RelSerSer> SerologyToSerologyRelationships { get; private set; }
        public IEnumerable<RelDnaSer> AlleleToSerologyRelationships { get; private set; }
        public IEnumerable<ConfidentialAllele> ConfidentialAlleles { get; private set; }
        public IEnumerable<AlleleStatus> AlleleStatuses { get; private set; }
        public IEnumerable<AlleleNameHistory> AlleleNameHistories { get; private set; }
        public IEnumerable<Dpb1TceGroup> Dpb1TceGroups { get; private set; }

        private readonly IWmdaFileReader wmdaFileReader;

        public WmdaDataRepository(IWmdaFileReader wmdaFileReader, string hlaDatabaseVersion)
        {
            this.wmdaFileReader = wmdaFileReader;
            HlaDatabaseVersion = hlaDatabaseVersion;
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
            AlleleNameHistories = GetWmdaData(new AlleleHistoryExtractor());
            Dpb1TceGroups = GetWmdaData(new Dpb1TceGroupExtractor());
        }

        private IEnumerable<TWmdaHlaTyping> GetWmdaData<TWmdaHlaTyping>(WmdaDataExtractor<TWmdaHlaTyping> extractor)
            where TWmdaHlaTyping : IWmdaHlaTyping
        {
            return extractor.GetWmdaHlaTypingsForPermittedLoci(wmdaFileReader, HlaDatabaseVersion);
        }
    }
}
