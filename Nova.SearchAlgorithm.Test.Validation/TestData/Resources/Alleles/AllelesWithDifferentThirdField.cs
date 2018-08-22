using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Resources
{
    /// <summary>
    /// This test data was manually curated from alleles found in DR_ANTIGENS in SOLAR
    /// It is used when we need to guarantee that a two-field match (with a differing third field) is possible
    ///
    /// No NMDP codes are known for the selected alleles, it is assumed this data will only be used for allele level resolution.
    /// If NMDP resolution tests are necessary, a new dataset will need to be curated
    /// </summary>
    public static class AllelesWithDifferentThirdFields
    {
        public static readonly LocusInfo<List<AlleleTestData>> Alleles = new LocusInfo<List<AlleleTestData>>
        {
            A = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*01:01:61"},
                new AlleleTestData {AlleleName = "*01:01:62"},
                new AlleleTestData {AlleleName = "*01:01:63"},
                new AlleleTestData {AlleleName = "*01:01:64"},
                new AlleleTestData {AlleleName = "*01:01:65"},
                new AlleleTestData {AlleleName = "*01:01:66"},
                new AlleleTestData {AlleleName = "*01:01:67"},
            },
            B = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*07:02:10"},
                new AlleleTestData {AlleleName = "*07:02:11"},
                new AlleleTestData {AlleleName = "*07:02:12"},
                new AlleleTestData {AlleleName = "*07:02:13"},
                new AlleleTestData {AlleleName = "*07:02:14"},
                new AlleleTestData {AlleleName = "*07:02:15"},
                new AlleleTestData {AlleleName = "*07:02:16"},
            },
            C = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*01:02:20"},
                new AlleleTestData {AlleleName = "*01:02:21"},
                new AlleleTestData {AlleleName = "*01:02:22"},
                new AlleleTestData {AlleleName = "*01:02:23"},
                new AlleleTestData {AlleleName = "*01:02:24"},
                new AlleleTestData {AlleleName = "*01:02:25"},
                new AlleleTestData {AlleleName = "*01:02:26"},
            },
            DPB1 = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*02:01:20"},
                new AlleleTestData {AlleleName = "*02:01:21"},
                new AlleleTestData {AlleleName = "*02:01:22"},
                new AlleleTestData {AlleleName = "*02:01:23"},
                new AlleleTestData {AlleleName = "*02:01:24"},
                new AlleleTestData {AlleleName = "*02:01:25"},
                new AlleleTestData {AlleleName = "*02:01:26"},
            },
            DQB1 = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*02:01:20"},
                new AlleleTestData {AlleleName = "*02:01:21"},
                new AlleleTestData {AlleleName = "*02:01:22"},
                new AlleleTestData {AlleleName = "*02:01:23"},
                new AlleleTestData {AlleleName = "*02:01:24"},
                new AlleleTestData {AlleleName = "*02:01:25"},
                new AlleleTestData {AlleleName = "*02:01:26"},
            },
            DRB1 = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*01:01:20"},
                new AlleleTestData {AlleleName = "*01:01:21"},
                new AlleleTestData {AlleleName = "*01:01:22"},
                new AlleleTestData {AlleleName = "*01:01:23"},
                new AlleleTestData {AlleleName = "*01:01:24"},
                new AlleleTestData {AlleleName = "*01:01:25"},
                new AlleleTestData {AlleleName = "*01:01:26"},
            },
        };
    }
}