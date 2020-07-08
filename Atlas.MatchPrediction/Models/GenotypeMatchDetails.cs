using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Models
{
    public class GenotypeMatchDetails
    {
        public PhenotypeInfo<string> PatientGenotype { get; set; }
        public PhenotypeInfo<string> DonorGenotype { get; set; }
        public LociInfo<int?> MatchCounts { get; set; }
        public int MatchCount => MatchCounts.Reduce((locus, value, accumulator) => accumulator + value ?? accumulator, 0);
    }
}
