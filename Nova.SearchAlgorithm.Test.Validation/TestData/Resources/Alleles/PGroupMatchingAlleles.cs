using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Resources
{
    /// <summary>
    /// This test data was manually curated from alleles found in hla_nom_p (v3330)
    /// It is used when we need to guarantee that a p-group level match grade is possible
    /// </summary>
    /// 
    /// This test data currently fulfils the criteria of being a non allele-level match, but fails at being p-group match grade,
    /// as the alleles that share a p-group also share a g-group
    // TODO: NOVA-1567: Find test data that has p-group match but not g-group match
    public static class PGroupMatchingAlleles
    {
        public static readonly PhenotypeInfo<List<AlleleTestData>> Alleles = new PhenotypeInfo<List<AlleleTestData>>
        {
            A_1 =
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*01:217", PGroup = "01:01P"},
                    new AlleleTestData {AlleleName = "*01:234", PGroup = "01:01P"},
                    new AlleleTestData {AlleleName = "*01:237", PGroup = "01:01P"},
                },
            A_2 =
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*01:132", PGroup = "01:01P"},
                    new AlleleTestData {AlleleName = "*01:253", PGroup = "01:01P"},
                    new AlleleTestData {AlleleName = "*01:261", PGroup = "01:01P"},
                },
            B_1 =
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*07:291", PGroup = "07:02P"},
                    new AlleleTestData {AlleleName = "*07:294", PGroup = "07:02P"},
                    new AlleleTestData {AlleleName = "*07:295", PGroup = "07:02P"},
                },
            B_2 =
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*07:298", PGroup = "07:02P"},
                    new AlleleTestData {AlleleName = "*07:308", PGroup = "07:02P"},
                    new AlleleTestData {AlleleName = "*07:311", PGroup = "07:02P"},
                },
            C_1 =
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*01:40", PGroup = "01:02P"},
                    new AlleleTestData {AlleleName = "*01:44", PGroup = "01:02P"},
                    new AlleleTestData {AlleleName = "*01:82", PGroup = "01:02P"},
                },
            C_2 =
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*01:85", PGroup = "01:02P"},
                    new AlleleTestData {AlleleName = "*01:127", PGroup = "01:02P"},
                    new AlleleTestData {AlleleName = "*01:135", PGroup = "01:02P"},
                },
            DPB1_1 =
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*702:01", PGroup = "04:01P"},
                    new AlleleTestData {AlleleName = "*699:01", PGroup = "04:01P"},
                    new AlleleTestData {AlleleName = "*677:01", PGroup = "04:01P"},
                },
            DPB1_2 =
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*670:01", PGroup = "04:01P"},
                    new AlleleTestData {AlleleName = "*618:01", PGroup = "04:01P"},
                    new AlleleTestData {AlleleName = "*615:01", PGroup = "04:01P"},
                },
            DQB1_1 =
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*02:106", PGroup = "02:01P"},
                    new AlleleTestData {AlleleName = "*02:105", PGroup = "02:01P"},
                    new AlleleTestData {AlleleName = "*02:102", PGroup = "02:01P"},
                },
            DQB1_2 =
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*02:89", PGroup = "02:01P"},
                    new AlleleTestData {AlleleName = "*02:80", PGroup = "02:01P"},
                    new AlleleTestData {AlleleName = "*02:59", PGroup = "02:01P"},
                },
            DRB1_1 =
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*09:31", PGroup = "09:01P"},
                    new AlleleTestData {AlleleName = "*09:21", PGroup = "09:01P"},
                },
            DRB1_2 =
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*09:33", PGroup = "09:01P"},
                    new AlleleTestData {AlleleName = "*09:01", PGroup = "09:01P"},
                },
        };
    }
}