// One haplotype's group names, one per locus. Same alias as FrequencyConsolidator / HaplotypeFrequencyCache /
// HaplotypeFrequencyService already use; the resolution is whatever HaplotypeFrequency.TypingCategory says per row.
using HfSetHaplotypeNames = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Models
{
    internal class Haplotype
    {
        public HfSetHaplotypeNames Hla { get; set; }
        public decimal Frequency { get; set; }
    }
}
