using System.Collections.Generic;

namespace Atlas.MatchPrediction.Models
{
    /// <summary>
    /// All possible states of phase for a genotype of unambiguous alleles.
    /// </summary>
    public class ExpandedGenotype
    {
        public IEnumerable<Diplotype> Diplotypes { get; set; }
        public bool IsHomozygousAtEveryLocus { get; set; }

    }
}
