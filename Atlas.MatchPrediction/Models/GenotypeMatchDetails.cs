using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

// Both come from GenotypeAtDesiredResolutions.HaplotypeResolution, so both are at the resolution the haplotype
// frequency set stored, category erased - NOT the string-matchable form the match counts were taken on.
using HfSetGenotypeNames = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;

namespace Atlas.MatchPrediction.Models
{
    public class GenotypeMatchDetails
    {
        public HfSetGenotypeNames PatientGenotype { get; set; }
        public decimal PatientGenotypeLikelihood { get; set; }
        public HfSetGenotypeNames DonorGenotype { get; set; }
        public decimal DonorGenotypeLikelihood { get; set; }
        public LociInfo<int?> MatchCounts { get; set; }
        public ISet<Locus> AvailableLoci { get; set; }
        public int MatchCount => MatchCounts.Reduce((_, value, accumulator) => accumulator + value ?? accumulator, 0);
        public int MismatchCount => (AvailableLoci.Count * 2) - MatchCount;
    }
}