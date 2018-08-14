using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Repositories
{
    public interface IAlleleRepository
    {
        PhenotypeInfo<List<AlleleTestData>> FourFieldAlleles();
        PhenotypeInfo<List<AlleleTestData>> FourFieldAllelesWithNonUniquePGroups();
    }

    /// <summary>
    /// Repository layer for accessing test allele data stored in Resources directory.
    /// </summary>
    public class AlleleRepository : IAlleleRepository
    {
        public PhenotypeInfo<List<AlleleTestData>> FourFieldAlleles()
        {
            return Resources.FourFieldAlleles.Alleles;
        }

        // TODO: NOVA-1567: Allow for 2 and 3 field alleles when testing based on p-group level matches
        public PhenotypeInfo<List<AlleleTestData>> FourFieldAllelesWithNonUniquePGroups()
        {
            return FourFieldAlleles().Map((l, p, alleles) =>
            {
                var pGroupGroups = alleles.GroupBy(a => a.PGroup).Where(g => g.Count() > 1).ToList();
                return alleles.Where(a => pGroupGroups.Any(g => g.Key == a.PGroup)).ToList();
            });
        }
    }
}