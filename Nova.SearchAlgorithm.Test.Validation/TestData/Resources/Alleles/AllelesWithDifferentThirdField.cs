using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Resources
{
    /// <summary>
    /// This test data was manually curated from 3-field TGS alleles found in the SOLAR database
    /// It is used when we need to guarantee that a two-field match (with a differing third field) is possible
    ///
    /// No NMDP codes are known for the selected alleles, it is assumed this data will only be used for allele level resolution.
    /// If NMDP resolution tests are necessary, a new dataset will need to be curated
    /// </summary>
    public static class AllelesWithDifferentThirdFields
    {
        // TODO: NOVA-1682: Ensure these are all full sequences to test for Protein matches
        public static readonly LocusInfo<List<AlleleTestData>> Alleles = new LocusInfo<List<AlleleTestData>>
        {
            A = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*02:01:01"},
                new AlleleTestData {AlleleName = "*02:01:04"},
                new AlleleTestData {AlleleName = "*02:01:05"},
                new AlleleTestData {AlleleName = "*02:01:09"},
                new AlleleTestData {AlleleName = "*11:01:01"},
                new AlleleTestData {AlleleName = "*11:01:02"},
                new AlleleTestData {AlleleName = "*11:01:27"},
            },
            B = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*07:02:01"},
                new AlleleTestData {AlleleName = "*07:02:03"},
                new AlleleTestData {AlleleName = "*07:02:10"},
                new AlleleTestData {AlleleName = "*07:02:13"},
                new AlleleTestData {AlleleName = "*08:01:01"},
                new AlleleTestData {AlleleName = "*08:01:02"},
                new AlleleTestData {AlleleName = "*08:01:20"},
            },
            C = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*03:04:01"},
                new AlleleTestData {AlleleName = "*03:04:02"},
                new AlleleTestData {AlleleName = "*03:04:10"},
                new AlleleTestData {AlleleName = "*03:04:18"},
                new AlleleTestData {AlleleName = "*05:01:01"},
                new AlleleTestData {AlleleName = "*05:01:05"},
                new AlleleTestData {AlleleName = "*05:01:08"},
            },
            DPB1 = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*01:01:01"},
                new AlleleTestData {AlleleName = "*01:01:02"},
                new AlleleTestData {AlleleName = "*01:01:03"},
                new AlleleTestData {AlleleName = "*02:01:02"},
                new AlleleTestData {AlleleName = "*02:01:04"},
                new AlleleTestData {AlleleName = "*02:01:20"},
            },
            DQB1 = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*03:01:01"},
                new AlleleTestData {AlleleName = "*03:01:02"},
                new AlleleTestData {AlleleName = "*03:01:08"},
                new AlleleTestData {AlleleName = "*03:01:04"},
                new AlleleTestData {AlleleName = "*06:02:01"},
                new AlleleTestData {AlleleName = "*06:02:02"},
                new AlleleTestData {AlleleName = "*06:02:07"},
            },
            DRB1 = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*01:01:01"},
                new AlleleTestData {AlleleName = "*01:01:02"},
                new AlleleTestData {AlleleName = "*01:01:16"},
                new AlleleTestData {AlleleName = "*03:01:01"},
                new AlleleTestData {AlleleName = "*03:01:02"},
                new AlleleTestData {AlleleName = "*03:01:08"},
            },
        };
    }
}