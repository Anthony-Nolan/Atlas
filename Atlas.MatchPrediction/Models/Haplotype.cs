using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Models
{
    internal class Haplotype
    {
        public LociInfo<string> Hla { get; set; }
        public decimal Frequency { get; set; }
    }
}
