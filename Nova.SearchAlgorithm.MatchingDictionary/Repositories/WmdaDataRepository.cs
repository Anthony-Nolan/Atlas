using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.Wmda;
using System;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IWmdaDataRepository
    {
        IEnumerable<HlaNom> HlaNomSerologies { get; }
        IEnumerable<HlaNom> HlaNomAlleles { get; }
        IEnumerable<HlaNomP> HlaNomP { get; }
        IEnumerable<HlaNomG> HlaNomG { get; }
        IEnumerable<RelSerSer> RelSerSer { get; }
        IEnumerable<RelDnaSer> RelDnaSer { get; }
        IEnumerable<ConfidentialAllele> ConfidentialAlleles { get; }
    }

    public class WmdaDataRepository : IWmdaDataRepository
    {
        private readonly IWmdaFileReader wmdaFileReader;

        public WmdaDataRepository(IWmdaFileReader wmdaFileReader)
        {
            this.wmdaFileReader = wmdaFileReader;
            PopulateWmdaDataCollections();
        }

        public IEnumerable<HlaNom> HlaNomSerologies { get; private set; }
        public IEnumerable<HlaNom> HlaNomAlleles { get; private set; }
        public IEnumerable<HlaNomP> HlaNomP { get; private set; }
        public IEnumerable<HlaNomG> HlaNomG { get; private set; }
        public IEnumerable<RelSerSer> RelSerSer { get; private set; }
        public IEnumerable<RelDnaSer> RelDnaSer { get; private set; }
        public IEnumerable<ConfidentialAllele> ConfidentialAlleles { get; private set; }

        private void PopulateWmdaDataCollections()
        {
            HlaNomSerologies = GetWmdaData<HlaNom>("wmda/hla_nom", SerologyFilter.Instance.Filter);
            HlaNomAlleles = GetWmdaData<HlaNom>("wmda/hla_nom", MolecularFilter.Instance.Filter);
            HlaNomP = GetWmdaData<HlaNomP>("wmda/hla_nom_p", MolecularFilter.Instance.Filter);
            HlaNomG = GetWmdaData<HlaNomG>("wmda/hla_nom_g", MolecularFilter.Instance.Filter);
            RelSerSer = GetWmdaData<RelSerSer>("wmda/rel_ser_ser", SerologyFilter.Instance.Filter);
            RelDnaSer = GetWmdaData<RelDnaSer>("wmda/rel_dna_ser", MolecularFilter.Instance.Filter);
            ConfidentialAlleles = GetWmdaData<ConfidentialAllele>("version_report", MolecularFilter.Instance.Filter);
        }

        private IEnumerable<TWmdaHlaTyping> GetWmdaData<TWmdaHlaTyping>(string fileName, Func<IWmdaHlaTyping, bool> filter) 
            where TWmdaHlaTyping : IWmdaHlaTyping
        {
            var fileContents = wmdaFileReader.GetFileContentsWithoutHeader(fileName);
            return WmdaDataFactory.GetWmdaData<TWmdaHlaTyping>(fileContents, filter);
        }
    }
}
