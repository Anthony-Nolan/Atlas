using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Test.Verification.Models
{
    internal class SimulantLocusHla
    {
        public Locus Locus { get; set; }
        public LocusInfo<string> HlaTyping { get; set; }
        public int GenotypeSimulantId { get; set; }
    }
}
