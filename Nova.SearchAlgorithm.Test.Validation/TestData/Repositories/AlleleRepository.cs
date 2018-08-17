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
        LocusInfo<List<AlleleTestData>> DonorAllelesForPGroupMatching();
        LocusInfo<AlleleTestData> PatientAllelesForPGroupMatching();
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

        public LocusInfo<List<AlleleTestData>> DonorAllelesForPGroupMatching()
        {
            return Resources.PGroupMatchingAlleles.DonorAlleles;
        }

        public LocusInfo<AlleleTestData> PatientAllelesForPGroupMatching()
        {
            return Resources.PGroupMatchingAlleles.PatientAlleles;
        }
    }
}