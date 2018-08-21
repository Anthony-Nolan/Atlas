using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Resources
{
    public static class NonMatchingAlleles
    {
        /// <summary>
        /// A set of alleles selected such that no loci match any other test data.
        /// TODO: NOVA-1590: Ensure that this assumption is true, and decide whether this is the best way to hanlde such non-matching cases
        /// </summary>
        public static readonly PhenotypeInfo<AlleleTestData> Alleles = new PhenotypeInfo<AlleleTestData>
        {
            A_1 = new AlleleTestData {AlleleName = "*74:02:01:02", NmdpCode = "*74:AZRC", Serology = "74"},
            A_2 = new AlleleTestData {AlleleName = "*74:02:01:02", NmdpCode = "*74:AZRC", Serology = "74"},
            B_1 = new AlleleTestData {AlleleName = "*78:01:01:02", NmdpCode = "*78:MS", Serology = "78"},
            B_2 = new AlleleTestData {AlleleName = "*78:01:01:02", NmdpCode = "*78:MS", Serology = "78"},
            C_1 = new AlleleTestData {AlleleName = "*18:01", NmdpCode = "*18:ZEPK", Serology = "18"},
            C_2 = new AlleleTestData {AlleleName = "*18:01", NmdpCode = "*18:ZEPK", Serology = "18"},
            DPB1_1 = new AlleleTestData {AlleleName = "*85:01:01:01", NmdpCode = "*85:RZN", Serology = ""},
            DPB1_2 = new AlleleTestData {AlleleName = "*85:01:01:01", NmdpCode = "*85:RZN", Serology = ""},
            DQB1_1 = new AlleleTestData {AlleleName = "*04:04", NmdpCode = "*04:DF", Serology = "4"},
            DQB1_2 = new AlleleTestData {AlleleName = "*04:04", NmdpCode = "*04:DF", Serology = "4"},
            DRB1_1 = new AlleleTestData {AlleleName = "*16:02:01:02", NmdpCode = "*16:AFB", Serology = "16"},
            DRB1_2 = new AlleleTestData {AlleleName = "*16:02:01:02", NmdpCode = "*16:AFB", Serology = "16"},
        };
    }
}