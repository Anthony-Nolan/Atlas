using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Resources
{
    /// <summary>
    /// This test data was manually curated from null expressing alleles found in the SOLAR database
    ///
    /// No NMDP codes are selected for the selected alleles, so this data can currently only be used for allele level resolution.
    /// When NMDP resolution tests are added, the dataset will need to be updated/replaced
    /// TODO: NOVA-1681: Add NMDP Codes
    /// </summary>
    public static class NullAlleles
    {
        public static readonly LocusInfo<List<AlleleTestData>> Alleles = new LocusInfo<List<AlleleTestData>>
        {
            A = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*01:04N"},
                new AlleleTestData {AlleleName = "*02:43N"},
                new AlleleTestData {AlleleName = "*24:36N"},
                new AlleleTestData {AlleleName = "*29:01:01:02N"},
            },
            B = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*15:01"},
                new AlleleTestData {AlleleName = "*15:01:01:02N"},
                new AlleleTestData {AlleleName = "*39:25N"},
                new AlleleTestData {AlleleName = "*51:27N"},
            },
            C = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*04:09N"},
                new AlleleTestData {AlleleName = "*05:07N"},
                new AlleleTestData {AlleleName = "*07:152N"},
                new AlleleTestData {AlleleName = "*07:02:01:17N"},
            },
            DPB1 = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*04:01:01:24N"},
                new AlleleTestData {AlleleName = "*61:01N"},
                new AlleleTestData {AlleleName = "*357:01N"},
                new AlleleTestData {AlleleName = "*450:01N"},
            },
            DQB1 = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*03:95N"},
                new AlleleTestData {AlleleName = "*04:46N"},
                new AlleleTestData {AlleleName = "*06:26N"},
                new AlleleTestData {AlleleName = "*06:158N"},
            },
            DRB1 = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*01:33N"},
                new AlleleTestData {AlleleName = "*07:10N"},
                new AlleleTestData {AlleleName = "*13:113N"},
                new AlleleTestData {AlleleName = "*15:17N"},
            },
        };
    }
}