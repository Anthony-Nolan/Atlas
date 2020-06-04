using System.Collections.Generic;

namespace Atlas.MatchPrediction.Models
{
    public class ImputedGenotype
    {
        public IEnumerable<Diplotype> Diplotypes { get; set; }
        public bool IsHomozygousAtEveryLocus { get; set; }

    }
}
