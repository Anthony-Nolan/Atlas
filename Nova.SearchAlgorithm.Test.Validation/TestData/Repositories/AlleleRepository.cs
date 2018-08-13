using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Repositories
{
    public class AlleleRepository
    {
        public static PhenotypeInfo<List<AlleleTestData>> FourFieldAlleles => Resources.FourFieldAlleles.Alleles;

        // TODO: NOVA-1567: Allow for 2 and 3 field alleles when testing based on p-group level matches
        public static PhenotypeInfo<List<AlleleTestData>> FourFieldAllelesWithNonUniquePGroups => FourFieldAlleles.Map((l, p, alleles) =>
        {
            var pGroupGroups = alleles.GroupBy(a => a.PGroup).Where(g => g.Count() > 1).ToList();
            return alleles.Where(a => pGroupGroups.Any(g => g.Key == a.PGroup)).ToList();
        });
    }
}