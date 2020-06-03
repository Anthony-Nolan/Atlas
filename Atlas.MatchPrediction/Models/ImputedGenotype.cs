using System.Collections.Generic;
using System.Linq;
using HaplotypeHla = Atlas.Common.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Models
{
    public class ImputedGenotype
    {
        public IEnumerable<Diplotype> Diplotypes { get; set; }
        public bool IsHomozygousAtEveryLocus { get; set; }

        public IEnumerable<HaplotypeHla> GetHaplotypes()
        {
            return Diplotypes.ToList().SelectMany(diplotype => new List<HaplotypeHla> {diplotype.Item1.Hla, diplotype.Item2.Hla});
        }
    }
}
