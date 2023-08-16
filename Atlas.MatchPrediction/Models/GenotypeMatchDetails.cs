using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Models
{
    public class GenotypeMatchDetails
    {
        public PhenotypeInfo<string> PatientGenotype { get; set; }
        public decimal PatientGenotypeLikelihood { get; set; }
        public PhenotypeInfo<string> DonorGenotype { get; set; }
        public decimal DonorGenotypeLikelihood { get; set; }
        public LociInfo<int?> MatchCounts { get; set; }
        public ISet<Locus> AvailableLoci { get; set; }
        public int MatchCount => MatchCounts.Reduce((locus, value, accumulator) => accumulator + value ?? accumulator, 0);
        public int MismatchCount => (AvailableLoci.Count * 2) - MatchCount;
    }
}