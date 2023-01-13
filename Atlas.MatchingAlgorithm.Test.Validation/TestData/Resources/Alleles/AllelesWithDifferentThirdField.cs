using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.Alleles
{
    /// <summary>
    /// This test data was manually curated full sequence alleles from Allele_status.txt (v3330)
    /// It is used when we need to guarantee that a two-field match (with a differing third field) is possible, and when a protein level match is required
    ///
    /// N.B. `Alleles with third field difference` !== `Protein level matches`.
    /// To be a protein level match, there must be a two field match *and* a full sequence.
    /// This data happens to fulfil both criteria, but it is possible to have a two field match with a different match grade
    /// 
    /// No NMDP codes are selected for the selected alleles, it is assumed this data will only be used for allele level resolution.
    /// If NMDP resolution tests are necessary, a new dataset will need to be curated
    /// </summary>
    public static class AllelesWithDifferentThirdFields
    {
        public static readonly LociInfo<List<AlleleTestData>> Alleles = new LociInfo<List<AlleleTestData>>
        (
            valueA: new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*01:01:71"},
                new AlleleTestData {AlleleName = "*01:01:72"},
                new AlleleTestData {AlleleName = "*01:01:73"},
                new AlleleTestData {AlleleName = "*01:01:77"},
                new AlleleTestData {AlleleName = "*01:01:78"},
                new AlleleTestData {AlleleName = "*01:01:79"},
                new AlleleTestData {AlleleName = "*11:01:72"},
                new AlleleTestData {AlleleName = "*11:01:73"},
                new AlleleTestData {AlleleName = "*11:01:74"},
                new AlleleTestData {AlleleName = "*11:01:75"},
            },
            valueB: new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*07:02:03"},
                new AlleleTestData {AlleleName = "*07:02:04"},
                new AlleleTestData {AlleleName = "*07:02:10"},
                new AlleleTestData {AlleleName = "*07:02:13"},
                new AlleleTestData {AlleleName = "*08:01:02"},
                new AlleleTestData {AlleleName = "*08:01:12"},
                new AlleleTestData {AlleleName = "*08:01:20"},
                new AlleleTestData {AlleleName = "*08:01:29"},
            },
            valueC: new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*03:04:02"},
                new AlleleTestData {AlleleName = "*03:04:04"},
                new AlleleTestData {AlleleName = "*03:04:13"},
                new AlleleTestData {AlleleName = "*03:04:18"},
                new AlleleTestData {AlleleName = "*05:01:02"},
                new AlleleTestData {AlleleName = "*05:01:05"},
                new AlleleTestData {AlleleName = "*05:01:16"},
                new AlleleTestData {AlleleName = "*05:01:32"},
            },
            valueDpb1: new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*02:01:03"},
                new AlleleTestData {AlleleName = "*02:01:04"},
                new AlleleTestData {AlleleName = "*02:01:08"},
                new AlleleTestData {AlleleName = "*02:01:15"},
                new AlleleTestData {AlleleName = "*04:01:31"},
                new AlleleTestData {AlleleName = "*04:01:33"},
                new AlleleTestData {AlleleName = "*04:01:34"},
                new AlleleTestData {AlleleName = "*04:01:35"},
            },
            valueDqb1: new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*03:01:03"},
                new AlleleTestData {AlleleName = "*03:01:08"},
                new AlleleTestData {AlleleName = "*03:01:17"},
                new AlleleTestData {AlleleName = "*03:01:22"},
                new AlleleTestData {AlleleName = "*06:02:11"},
                new AlleleTestData {AlleleName = "*06:02:17"},
                new AlleleTestData {AlleleName = "*06:02:22"},
                new AlleleTestData {AlleleName = "*06:02:23"},
            },
            valueDrb1: new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*13:01:19"},
                new AlleleTestData {AlleleName = "*13:01:20"},
                new AlleleTestData {AlleleName = "*13:01:21"},
                new AlleleTestData {AlleleName = "*13:01:22"},
                new AlleleTestData {AlleleName = "*14:04:01"},
                new AlleleTestData {AlleleName = "*14:04:04"},
                new AlleleTestData {AlleleName = "*14:04:05"},
                new AlleleTestData {AlleleName = "*14:04:06"},
            }
        );
    }
}