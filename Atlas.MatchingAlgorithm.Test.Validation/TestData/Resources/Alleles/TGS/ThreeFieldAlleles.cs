using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.Alleles.TGS
{
    /// <summary>
    /// This test data was manually curated from 3-field TGS typed alleles found in the SOLAR database
    /// A corresponding NMDP code was selected for each allele from the DR_ANTIGENS table
    /// (most alleles will correspond to multiple NMDP codes - only one is necessary for testing purposes)
    /// The corresponding serology, p-group, and g-group data was retrieved from the wmda files: hla_nom_g, hla_nom_p, rel_dna_ser (v3330)
    ///
    /// This dataset may be amended, provided all data:
    /// (1) Is a 3-field, TGS typed allele
    /// (2) Has the correct p-group, g-group, and serology associations
    /// (3) Has a valid corresponding nmdp code
    /// </summary>
    public static class ThreeFieldAlleles
    {
        public static readonly PhenotypeInfo<List<AlleleTestData>> Alleles = new PhenotypeInfo<List<AlleleTestData>>
        (
            valueA: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData
                        {AlleleName = "*02:03:01", PGroup = "02:03P", GGroup = "02:03:01G", NmdpCode = "*02:JXF", Serology = "203"},
                    new AlleleTestData {AlleleName = "*02:07:01", PGroup = "02:07P", GGroup = "02:07:01G", NmdpCode = "*02:HXA", Serology = "2"},
                    new AlleleTestData {AlleleName = "*02:11:01", PGroup = "02:11P", GGroup = "02:11:01G", NmdpCode = "*02:HRB", Serology = "2"},
                    new AlleleTestData {AlleleName = "*02:17:02", PGroup = "02:17P", GGroup = "02:17:01G", NmdpCode = "*02:ATDU", Serology = "2"},
                    new AlleleTestData {AlleleName = "*03:02:01", PGroup = "03:02P", GGroup = "03:02:01G", NmdpCode = "*03:XE", Serology = "3"},
                    new AlleleTestData {AlleleName = "*11:02:01", PGroup = "11:02P", GGroup = "11:02:01G", NmdpCode = "*11:US", Serology = "11"},
                    new AlleleTestData {AlleleName = "*24:02:13", PGroup = "24:02P", GGroup = "24:02:01G", NmdpCode = "*24:HGF", Serology = "24"},
                    new AlleleTestData {AlleleName = "*24:02:40", PGroup = "24:02P", GGroup = "24:02:01G", NmdpCode = "*24:HGF", Serology = "24"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*02:01:11", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:UE", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:03:01", PGroup = "02:03P", GGroup = "02:03:01G", NmdpCode = "*02:JXF", Serology = "203"},
                    new AlleleTestData {AlleleName = "*02:07:01", PGroup = "02:07P", GGroup = "02:07:01G", NmdpCode = "*02:HXA", Serology = "2"},
                    new AlleleTestData {AlleleName = "*02:11:01", PGroup = "02:11P", GGroup = "02:11:01G", NmdpCode = "*02:HRB", Serology = "2"},
                    new AlleleTestData {AlleleName = "*02:17:02", PGroup = "02:17P", GGroup = "02:17:01G", NmdpCode = "*02:ATDU", Serology = "2"},
                    new AlleleTestData {AlleleName = "*02:97:01", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:EJHG", Serology = "2"},
                    new AlleleTestData {AlleleName = "*03:02:01", PGroup = "03:02P", GGroup = "03:02:01G", NmdpCode = "*03:XE", Serology = "3"},
                    new AlleleTestData {AlleleName = "*11:02:01", PGroup = "11:02P", GGroup = "11:02:01G", NmdpCode = "*11:US", Serology = "11"},
                    new AlleleTestData {AlleleName = "*24:02:13", PGroup = "24:02P", GGroup = "24:02:01G", NmdpCode = "*24:HGF", Serology = "24"},
                }
            ),
            valueB: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*07:02:45", PGroup = "07:02P", GGroup = "07:02:01G", NmdpCode = "*07:KT", Serology = "7"},
                    new AlleleTestData {AlleleName = "*07:06:01", PGroup = "07:05P", GGroup = "07:05:01G", NmdpCode = "*07:ABUN", Serology = "7"},
                    new AlleleTestData {AlleleName = "*08:01:20", PGroup = "08:01P", GGroup = "08:01:01G", NmdpCode = "*08:AC", Serology = "8"},
                    new AlleleTestData {AlleleName = "*15:02:01", PGroup = "15:02P", GGroup = "15:02:01G", NmdpCode = "*15:DCJ", Serology = "75"},
                    new AlleleTestData {AlleleName = "*15:11:01", PGroup = "15:11P", GGroup = "15:11:01G", NmdpCode = "*15:CUY", Serology = "75"},
                    new AlleleTestData {AlleleName = "*15:25:01", PGroup = "15:25P", GGroup = "15:25:01G", NmdpCode = "*15:DSV", Serology = "62"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*07:02:45", PGroup = "07:02P", GGroup = "07:02:01G", NmdpCode = "*07:KT", Serology = "7"},
                    new AlleleTestData {AlleleName = "*07:06:01", PGroup = "07:05P", GGroup = "07:05:01G", NmdpCode = "*07:ABUN", Serology = "7"},
                    new AlleleTestData {AlleleName = "*08:01:20", PGroup = "08:01P", GGroup = "08:01:01G", NmdpCode = "*08:AC", Serology = "8"},
                    new AlleleTestData {AlleleName = "*15:02:01", PGroup = "15:02P", GGroup = "15:02:01G", NmdpCode = "*15:DCJ", Serology = "75"},
                    new AlleleTestData {AlleleName = "*15:09:01", PGroup = "15:09P", GGroup = "15:09:01G", NmdpCode = "*15:EEZ", Serology = "70"},
                    new AlleleTestData {AlleleName = "*15:11:01", PGroup = "15:11P", GGroup = "15:11:01G", NmdpCode = "*15:CUY", Serology = "75"},
                    new AlleleTestData {AlleleName = "*15:25:01", PGroup = "15:25P", GGroup = "15:25:01G", NmdpCode = "*15:DSV", Serology = "62"},
                }
            ),
            valueC: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*02:14:01", PGroup = "02:14P", GGroup = "02:14:01G", NmdpCode = "*02:VYJ", Serology = "2"},
                    new AlleleTestData {AlleleName = "*02:14:02", PGroup = "02:14P", GGroup = "02:14:01G", NmdpCode = "*02:VYJ", Serology = "2"},
                    new AlleleTestData {AlleleName = "*03:02:01", PGroup = "03:02P", GGroup = "03:02:01G", NmdpCode = "*03:BF", Serology = "10"},
                    new AlleleTestData {AlleleName = "*03:02:07", PGroup = "03:02P", GGroup = "03:02:01G", NmdpCode = "*03:BF", Serology = "10"},
                    new AlleleTestData {AlleleName = "*03:04:02", PGroup = "03:04P", GGroup = "03:04:02G", NmdpCode = "*03:HCD", Serology = "10"},
                    new AlleleTestData {AlleleName = "*04:01:79", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:GC", Serology = "4"},
                    new AlleleTestData {AlleleName = "*04:01:82", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:GC", Serology = "4"},
                    new AlleleTestData {AlleleName = "*04:03:01", PGroup = "04:03P", GGroup = "04:03:01G", NmdpCode = "*04:CF", Serology = "4"},
                    new AlleleTestData {AlleleName = "*07:01:02", PGroup = "07:01P", GGroup = "07:01:01G", NmdpCode = "*07:CZB", Serology = "7"},
                    new AlleleTestData {AlleleName = "*07:02:80", PGroup = "07:02P", GGroup = "07:02:01G", NmdpCode = "*07:FJF", Serology = "7"},
                    new AlleleTestData {AlleleName = "*08:03:01", PGroup = "08:03P", GGroup = "08:03:01G", NmdpCode = "*08:CNY", Serology = "8"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*03:04:02", PGroup = "03:04P", GGroup = "03:04:02G", NmdpCode = "*03:HCD", Serology = "10"},
                    new AlleleTestData {AlleleName = "*04:01:79", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:GC", Serology = "4"},
                    new AlleleTestData {AlleleName = "*04:03:01", PGroup = "04:03P", GGroup = "04:03:01G", NmdpCode = "*04:CF", Serology = "4"},
                    new AlleleTestData {AlleleName = "*07:01:02", PGroup = "07:01P", GGroup = "07:01:01G", NmdpCode = "*07:CZB", Serology = "7"},
                    new AlleleTestData {AlleleName = "*07:01:09", PGroup = "07:01P", GGroup = "07:01:01G", NmdpCode = "*07:CZB", Serology = "7"},
                    new AlleleTestData {AlleleName = "*07:02:80", PGroup = "07:02P", GGroup = "07:02:01G", NmdpCode = "*07:FJF", Serology = "7"},
                    new AlleleTestData {AlleleName = "*08:03:01", PGroup = "08:03P", GGroup = "08:03:01G", NmdpCode = "*08:CNY", Serology = "8"},
                }
            ),
            // Note that none of the DPB1 TGS 3-field alleles have an nmdp or serology association.
            // DPB1 cannot be tested at these resolutions at the three field level
            valueDpb1: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*01:01:01"},
                    new AlleleTestData {AlleleName = "*01:01:02"},
                    new AlleleTestData {AlleleName = "*02:01:04"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*01:01:01"},
                    new AlleleTestData {AlleleName = "*01:01:02"},
                    new AlleleTestData {AlleleName = "*02:01:04"},
                }
            ),
            valueDqb1: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*02:01:01", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:AB", Serology = "2"},
                    new AlleleTestData {AlleleName = "*03:01:04", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:HGB", Serology = "7"},
                    new AlleleTestData {AlleleName = "*03:04:01", PGroup = "03:04P", GGroup = "03:04:01G", NmdpCode = "*03:CSU", Serology = "7"},
                    new AlleleTestData {AlleleName = "*03:05:01", PGroup = "03:05P", GGroup = "03:05:01G", NmdpCode = "*03:ARH", Serology = "8"},
                    new AlleleTestData {AlleleName = "*03:19:01", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:AVU", Serology = "3"},
                    new AlleleTestData {AlleleName = "*06:01:03", PGroup = "06:01P", GGroup = "06:01:01G", NmdpCode = "*06:UE", Serology = "6"},
                    new AlleleTestData {AlleleName = "*06:04:01", PGroup = "06:04P", GGroup = "06:04:01G", NmdpCode = "*06:BFZ", Serology = "6"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*02:01:01", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:AB", Serology = "2"},
                    new AlleleTestData {AlleleName = "*03:01:04", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:HGB", Serology = "7"},
                    new AlleleTestData {AlleleName = "*03:03:04", PGroup = "03:03P", GGroup = "03:03:02G", NmdpCode = "*03:CD", Serology = "9"},
                    new AlleleTestData {AlleleName = "*03:04:01", PGroup = "03:04P", GGroup = "03:04:01G", NmdpCode = "*03:CSU", Serology = "7"},
                    new AlleleTestData {AlleleName = "*03:05:01", PGroup = "03:05P", GGroup = "03:05:01G", NmdpCode = "*03:ARH", Serology = "8"},
                    new AlleleTestData {AlleleName = "*03:19:01", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:AVU", Serology = "3"},
                    new AlleleTestData {AlleleName = "*06:01:03", PGroup = "06:01P", GGroup = "06:01:01G", NmdpCode = "*06:UE", Serology = "6"},
                    new AlleleTestData {AlleleName = "*06:02:23", PGroup = "06:02P", GGroup = "06:02:01G", NmdpCode = "*06:CWV", Serology = "6"},
                    new AlleleTestData {AlleleName = "*06:04:01", PGroup = "06:04P", GGroup = "06:04:01G", NmdpCode = "*06:BFZ", Serology = "6"},
                }
            ),
            valueDrb1: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*01:01:01", PGroup = "01:01P", GGroup = "01:01:01G", NmdpCode = "*01:MP", Serology = "1"},
                    new AlleleTestData {AlleleName = "*01:02:01", PGroup = "01:02P", GGroup = "01:02:01G", NmdpCode = "*01:BF", Serology = "1"},
                    new AlleleTestData {AlleleName = "*03:01:08", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:VG", Serology = "17"},
                    new AlleleTestData {AlleleName = "*04:06:01", PGroup = "04:06P", GGroup = "04:06:01G", NmdpCode = "*04:MCC", Serology = "4"},
                    new AlleleTestData {AlleleName = "*04:06:02", PGroup = "04:06P", GGroup = "04:06:01G", NmdpCode = "*04:MCC", Serology = "4"},
                    new AlleleTestData {AlleleName = "*11:01:08", PGroup = "11:01P", GGroup = "11:01:01G", NmdpCode = "*11:AHU", Serology = "11"},
                    new AlleleTestData {AlleleName = "*11:04:01", PGroup = "11:04P", GGroup = "11:04:01G", NmdpCode = "*11:HTC", Serology = "11"},
                    new AlleleTestData {AlleleName = "*11:06:01", PGroup = "11:06P", GGroup = "11:06:01G", NmdpCode = "*11:BFH", Serology = "11"},
                    new AlleleTestData {AlleleName = "*11:11:01", PGroup = "11:11P", GGroup = "11:11:01G", NmdpCode = "*11:DY", Serology = "11"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*01:01:01", PGroup = "01:01P", GGroup = "01:01:01G", NmdpCode = "*01:MP", Serology = "1"},
                    new AlleleTestData {AlleleName = "*01:02:01", PGroup = "01:02P", GGroup = "01:02:01G", NmdpCode = "*01:BF", Serology = "1"},
                    new AlleleTestData {AlleleName = "*03:01:08", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:VG", Serology = "17"},
                    new AlleleTestData {AlleleName = "*04:06:01", PGroup = "04:06P", GGroup = "04:06:01G", NmdpCode = "*04:MCC", Serology = "4"},
                    new AlleleTestData {AlleleName = "*04:06:02", PGroup = "04:06P", GGroup = "04:06:01G", NmdpCode = "*04:MCC", Serology = "4"},
                    new AlleleTestData {AlleleName = "*11:01:08", PGroup = "11:01P", GGroup = "11:01:01G", NmdpCode = "*11:AHU", Serology = "11"},
                    new AlleleTestData {AlleleName = "*11:04:01", PGroup = "11:04P", GGroup = "11:04:01G", NmdpCode = "*11:HTC", Serology = "11"},
                    new AlleleTestData {AlleleName = "*11:06:01", PGroup = "11:06P", GGroup = "11:06:01G", NmdpCode = "*11:BFH", Serology = "11"},
                    new AlleleTestData {AlleleName = "*11:11:01", PGroup = "11:11P", GGroup = "11:11:01G", NmdpCode = "*11:DY", Serology = "11"},
                    new AlleleTestData {AlleleName = "*11:13:02", PGroup = "11:13P", GGroup = "11:13:01G", NmdpCode = "*11:YP", Serology = "11"},
                }
            )
        );
    }
}