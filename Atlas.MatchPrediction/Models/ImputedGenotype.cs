using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Models
{
    public class ImputedGenotype
    {
        public IEnumerable<Diplotype> Diplotypes { get; set; }
        public bool IsHomozygousAtEveryLocus { get; set; }

        public IEnumerable<LociInfo<string>> GetHaplotypes()
        {
            return Diplotypes.ToList().SelectMany(diplotype => new List<LociInfo<string>>
                {diplotype.Item1.Hla, diplotype.Item2.Hla});
        }
    }
}
