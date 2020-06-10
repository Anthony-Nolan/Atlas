using System.Collections.Generic;

namespace Atlas.MatchPrediction.Models
{
    public class ExpandedGenotype
    {
        public IEnumerable<Diplotype> Diplotypes { get; set; }
        public bool IsHomozygousAtEveryLocus { get; set; }

    }
}
