using Atlas.Common.GeneticData;

namespace Atlas.MatchingAlgorithm.Clients.Http.HlaService.Models
{
    public class Antigen
    {
        public Locus Locus { get; set; }
        public string HlaName { get; set; }
        public string NmdpString { get; set; }
    }
}