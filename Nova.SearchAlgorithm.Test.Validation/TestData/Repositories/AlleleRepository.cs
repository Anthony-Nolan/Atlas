using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Repositories
{
    public interface IAlleleRepository
    {
        PhenotypeInfo<List<AlleleTestData>> FourFieldAlleles();
        PhenotypeInfo<List<AlleleTestData>> ThreeFieldAlleles();
        PhenotypeInfo<List<AlleleTestData>> TwoFieldAlleles();
        PhenotypeInfo<List<AlleleTestData>> AllelesWithNonUniquePGroups();
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
        
        public PhenotypeInfo<List<AlleleTestData>> ThreeFieldAlleles()
        {
            return Resources.ThreeFieldAlleles.Alleles;
        }
        
        public PhenotypeInfo<List<AlleleTestData>> TwoFieldAlleles()
        {
            return Resources.TwoFieldAlleles.Alleles;
        }

        public PhenotypeInfo<List<AlleleTestData>> AllelesWithNonUniquePGroups()
        {
            return Resources.PGroupMatchingAlleles.Alleles.Map((l, p, alleles) =>
            {
                var pGroupGroups = alleles.GroupBy(a => a.PGroup).Where(g => g.Count() > 1).ToList();
                return alleles.Where(a => pGroupGroups.Any(g => g.Key == a.PGroup)).ToList();
            });
        }
    }
}