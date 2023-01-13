using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.Alleles
{
    public static class NonMatchingAlleles
    {
        /// <summary>
        /// A set of alleles selected such that no loci match any other test data.
        /// To be used when patient should not match a meta-donor's data
        /// </summary>
        public static readonly LociInfo<AlleleTestData> NonMatchingPatientAlleles = new LociInfo<AlleleTestData>
        (
            valueA: new AlleleTestData {AlleleName = "*74:02:01:02", NmdpCode = "*74:AZRC", Serology = "74"},
            valueB: new AlleleTestData {AlleleName = "*78:01:01:02", NmdpCode = "*78:MS", Serology = "78"},
            valueC: new AlleleTestData {AlleleName = "*18:01", NmdpCode = "*18:ZEPK", Serology = "18"},
            valueDpb1: new AlleleTestData {AlleleName = "*85:01:01:01", NmdpCode = "*85:RZN"},
            valueDqb1: new AlleleTestData {AlleleName = "*04:04", NmdpCode = "*04:DF", Serology = "4"},
            valueDrb1: new AlleleTestData {AlleleName = "*16:02:01:02", NmdpCode = "*16:AFB", Serology = "16"}
        );

        /// <summary>
        /// A set of alleles selected such that no loci match any other test data.
        /// To be used when database donor should not match a meta-donor's data
        /// Distinct from patient non-matching alleles, so that we still don't get a match when both patient and DB-donor do not match meta-donor
        /// </summary>
        public static readonly LociInfo<AlleleTestData> NonMatchingDonorAlleles = new LociInfo<AlleleTestData>
        (
            valueA: new AlleleTestData {AlleleName = "*69:03"},
            valueB: new AlleleTestData {AlleleName = "*83:01"},
            valueC: new AlleleTestData {AlleleName = "*17:37"},
            valueDpb1: new AlleleTestData {AlleleName = "*156:01"},
            valueDqb1: new AlleleTestData {AlleleName = "*05:149"},
            valueDrb1: new AlleleTestData {AlleleName = "*08:64"}
        );

        /// <summary>
        /// A set of null alleles selected such that none are included in the null allele test data set
        /// Used when testing matching behaviour of different null alleles
        /// </summary>
        public static readonly LociInfo<AlleleTestData> NonMatchingNullAlleles = new LociInfo<AlleleTestData>
        (
            valueA: new AlleleTestData {AlleleName = "*23:07N"},
            valueB: new AlleleTestData {AlleleName = "*44:19N"},
            valueC: new AlleleTestData {AlleleName = "*14:07N"},
            valueDpb1: new AlleleTestData {AlleleName = "*401:01N"},
            valueDqb1: new AlleleTestData {AlleleName = "*05:90N"},
            valueDrb1: new AlleleTestData {AlleleName = "*12:24N"}
        );
    }
}