using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.Alleles.MatchGrades
{
    /// <summary>
    /// This test data was manually curated from alleles found in hla_nom_g (v3330)
    /// It is used when we need to guarantee that a g-group level match grade is possible
    ///
    /// For each locus, a single g-group was selected, from which various alleles with differing first two fields were chosen
    /// </summary>
    public static class GGroupMatchingAlleles
    {
        public static readonly PhenotypeInfo<List<AlleleTestData>> Alleles = new PhenotypeInfo<List<AlleleTestData>>
        (
            valueA: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*01:261", GGroup = "01:01:01G"},
                    new AlleleTestData {AlleleName = "*01:253", GGroup = "01:01:01G"},
                    new AlleleTestData {AlleleName = "*01:252", GGroup = "01:01:01G"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*01:251", GGroup = "01:01:01G"},
                    new AlleleTestData {AlleleName = "*01:248", GGroup = "01:01:01G"},
                    new AlleleTestData {AlleleName = "*01:246", GGroup = "01:01:01G"},
                }
            ),
            valueB: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*07:312", GGroup = "07:02:01G"},
                    new AlleleTestData {AlleleName = "*07:130", GGroup = "07:02:01G"},
                    new AlleleTestData {AlleleName = "*07:02:01:01", GGroup = "07:02:01G"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*07:58", GGroup = "07:02:01G"},
                    new AlleleTestData {AlleleName = "*07:156", GGroup = "07:02:01G"},
                    new AlleleTestData {AlleleName = "*07:129", GGroup = "07:02:01G"},
                }
            ),
            valueC: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*01:155", GGroup = "01:02:01G"},
                    new AlleleTestData {AlleleName = "*01:02:01:09", GGroup = "01:02:01G"},
                    new AlleleTestData {AlleleName = "*01:25", GGroup = "01:02:01G"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*01:83", GGroup = "01:02:01G"},
                    new AlleleTestData {AlleleName = "*01:44", GGroup = "01:02:01G"},
                    new AlleleTestData {AlleleName = "*01:135", GGroup = "01:02:01G"},
                }
            ),
            valueDpb1: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*678:01", GGroup = "02:01:02G"},
                    new AlleleTestData {AlleleName = "*617:01", GGroup = "02:01:02G"},
                    new AlleleTestData {AlleleName = "*416:01:01:02", GGroup = "02:01:02G"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*02:01:02:02", GGroup = "02:01:02G"},
                    new AlleleTestData {AlleleName = "*352:01", GGroup = "02:01:02G"},
                    new AlleleTestData {AlleleName = "*414:01:01:01", GGroup = "02:01:02G"},
                }
            ),
            valueDqb1: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*02:01:01", GGroup = "02:01:01G"},
                    new AlleleTestData {AlleleName = "*02:06", GGroup = "02:01:01G"},
                    new AlleleTestData {AlleleName = "*02:09", GGroup = "02:01:01G"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*02:02:01:02", GGroup = "02:01:01G"},
                    new AlleleTestData {AlleleName = "*02:59", GGroup = "02:01:01G"},
                    new AlleleTestData {AlleleName = "*02:105", GGroup = "02:01:01G"},
                }
            ),
            valueDrb1: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*03:144", GGroup = "03:01:01G"},
                    new AlleleTestData {AlleleName = "*03:146", GGroup = "03:01:01G"},
                    new AlleleTestData {AlleleName = "*03:137", GGroup = "03:01:01G"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*03:01:26", GGroup = "03:01:01G"},
                    new AlleleTestData {AlleleName = "*03:124", GGroup = "03:01:01G"},
                    new AlleleTestData {AlleleName = "*03:132", GGroup = "03:01:01G"},
                }
            )
        );
    };
}