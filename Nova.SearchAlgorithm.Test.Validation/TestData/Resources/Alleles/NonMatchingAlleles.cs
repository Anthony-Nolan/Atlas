using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Resources
{
    public static class NonMatchingAlleles
    {
        /// <summary>
        /// A set of alleles selected such that no loci match any other test data.
        /// To be used when patient should not match a meta-donor's data
        /// </summary>
        public static readonly LocusInfo<AlleleTestData> NonMatchingPatientAlleles = new LocusInfo<AlleleTestData>
        {
            A = new AlleleTestData {AlleleName = "*74:02:01:02", NmdpCode = "*74:AZRC", Serology = "74"},
            B = new AlleleTestData {AlleleName = "*78:01:01:02", NmdpCode = "*78:MS", Serology = "78"},
            C = new AlleleTestData {AlleleName = "*18:01", NmdpCode = "*18:ZEPK", Serology = "18"},
            DPB1 = new AlleleTestData {AlleleName = "*85:01:01:01", NmdpCode = "*85:RZN", Serology = ""},
            DQB1 = new AlleleTestData {AlleleName = "*04:04", NmdpCode = "*04:DF", Serology = "4"},
            DRB1 = new AlleleTestData {AlleleName = "*16:02:01:02", NmdpCode = "*16:AFB", Serology = "16"},
        };

        /// <summary>
        /// A set of alleles selected such that no loci match any other test data.
        /// To be used when database donor should not match a meta-donor's data
        /// Distinct from patient non-matching alleles, so that we still don't get a match when both patient and DB-donor so not match DB Donor
        /// </summary>
        public static readonly LocusInfo<AlleleTestData> NonMatchingDonorAlleles = new LocusInfo<AlleleTestData>
        {
            A = new AlleleTestData {AlleleName = "*69:03"},
            B = new AlleleTestData {AlleleName = "*83:01"},
            C = new AlleleTestData {AlleleName = "*17:37"},
            DPB1 = new AlleleTestData {AlleleName = "*156:01"},
            DQB1 = new AlleleTestData {AlleleName = "*05:149"},
            DRB1 = new AlleleTestData {AlleleName = "*08:64"},
        };
    }
}