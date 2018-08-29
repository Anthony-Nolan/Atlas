using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Resources
{
    public static class NonMatchingAlleles
    {
        /// <summary>
        /// A set of alleles selected such that no loci match any other test data.
        /// </summary>
        public static readonly LocusInfo<AlleleTestData> Alleles = new LocusInfo<AlleleTestData>
        {
            A = new AlleleTestData {AlleleName = "*74:02:01:02", NmdpCode = "*74:AZRC", Serology = "74"},
            B = new AlleleTestData {AlleleName = "*78:01:01:02", NmdpCode = "*78:MS", Serology = "78"},
            C = new AlleleTestData {AlleleName = "*18:01", NmdpCode = "*18:ZEPK", Serology = "18"},
            DPB1 = new AlleleTestData {AlleleName = "*85:01:01:01", NmdpCode = "*85:RZN", Serology = ""},
            DQB1 = new AlleleTestData {AlleleName = "*04:04", NmdpCode = "*04:DF", Serology = "4"},
            DRB1 = new AlleleTestData {AlleleName = "*16:02:01:02", NmdpCode = "*16:AFB", Serology = "16"},
        };
    }
}