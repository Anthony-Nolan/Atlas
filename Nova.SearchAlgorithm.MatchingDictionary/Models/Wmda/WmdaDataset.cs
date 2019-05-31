using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda
{
    public class WmdaDataset
    {
        public string HlaDatabaseVersion { get; set; }
        public IEnumerable<HlaNom> Serologies { get; set; }
        public IEnumerable<HlaNom> Alleles { get; set; }
        public IEnumerable<HlaNomP> PGroups { get; set; }
        public IEnumerable<HlaNomG> GGroups { get; set; }
        public IEnumerable<RelSerSer> SerologyToSerologyRelationships { get; set; }
        public IEnumerable<RelDnaSer> AlleleToSerologyRelationships { get; set; }
        public IEnumerable<ConfidentialAllele> ConfidentialAlleles { get; set; }
        public IEnumerable<AlleleStatus> AlleleStatuses { get; set; }
        public IEnumerable<AlleleNameHistory> AlleleNameHistories { get; set; }
        public IEnumerable<Dpb1TceGroupAssignment> Dpb1TceGroupAssignments { get; set; }
    }
}