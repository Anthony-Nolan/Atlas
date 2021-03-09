using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.Alleles
{
    /// <summary>
    /// This test data was manually curated from null expressing alleles found in the SOLAR database
    ///
    /// No NMDP codes are selected for the selected alleles, so this data can currently only be used for allele level resolution.
    /// When NMDP resolution tests are added, the dataset will need to be updated/replaced
    /// </summary>
    public static class NullAlleles
    {
        public static readonly LociInfo<List<AlleleTestData>> Alleles = new LociInfo<List<AlleleTestData>>
        (
            valueA: new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*01:04N"},
                new AlleleTestData {AlleleName = "*02:43N"},
                new AlleleTestData {AlleleName = "*24:36N"},
                new AlleleTestData {AlleleName = "*29:01:01:02N"},
            },
            valueB: new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*15:01"},
                new AlleleTestData {AlleleName = "*15:01:01:02N"},
                new AlleleTestData {AlleleName = "*39:25N"},
                new AlleleTestData {AlleleName = "*51:27N"},
            },
            valueC: new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*04:09N"},
                new AlleleTestData {AlleleName = "*05:07N"},
                new AlleleTestData {AlleleName = "*07:152N"},
                new AlleleTestData {AlleleName = "*07:02:01:17N"},
            },
            valueDpb1: new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*04:01:01:24N"},
                new AlleleTestData {AlleleName = "*61:01N"},
                new AlleleTestData {AlleleName = "*357:01N"},
                new AlleleTestData {AlleleName = "*450:01N"},
            },
            valueDqb1: new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*03:95N"},
                new AlleleTestData {AlleleName = "*04:46N"},
                new AlleleTestData {AlleleName = "*06:26N"},
                new AlleleTestData {AlleleName = "*06:158N"},
            },
            valueDrb1: new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*01:33N"},
                new AlleleTestData {AlleleName = "*07:10N"},
                new AlleleTestData {AlleleName = "*13:113N"},
                new AlleleTestData {AlleleName = "*15:17N"},
            }
        );
    }
}