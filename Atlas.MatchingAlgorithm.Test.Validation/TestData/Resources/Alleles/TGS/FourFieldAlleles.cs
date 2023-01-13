using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.Alleles.TGS
{
    /// <summary>
    /// This test data was manually curated from 4-field TGS typed alleles found in the SOLAR database
    /// A corresponding NMDP code was selected for each allele from the DR_ANTIGENS table
    /// (most alleles will correspond to multiple NMDP codes - only one is necessary for testing purposes)
    /// The corresponding serology, p-group, and g-group data was retrieved from the wmda files: hla_nom_g, hla_nom_p, rel_dna_ser (v3330)
    ///
    /// This dataset may be amended, provided all data:
    /// (1) Is a 4-field, TGS typed allele
    /// (2) Has the correct p-group, g-group, and serology associations
    /// (3) Has a valid corresponding nmdp code
    /// </summary>
    public static class FourFieldAlleles
    {
        public static readonly PhenotypeInfo<List<AlleleTestData>> Alleles = new PhenotypeInfo<List<AlleleTestData>>
        (
            valueA: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData
                        {AlleleName = "*01:01:01:01", PGroup = "01:01P", GGroup = "01:01:01G", NmdpCode = "*01:AG", Serology = "1"},
                    new AlleleTestData
                        {AlleleName = "*01:01:01:03", PGroup = "01:01P", GGroup = "01:01:01G", NmdpCode = "*01:AG", Serology = "1"},
                    new AlleleTestData
                        {AlleleName = "*01:01:01:09", PGroup = "01:01P", GGroup = "01:01:01G", NmdpCode = "*01:AG", Serology = "1"},
                    new AlleleTestData
                        {AlleleName = "*01:03:01:01", PGroup = "01:03P", GGroup = "01:03:01G", NmdpCode = "*01:CJ", Serology = "1"},
                    new AlleleTestData
                        {AlleleName = "*01:03:01:02", PGroup = "01:03P", GGroup = "01:03:01G", NmdpCode = "*01:CJ", Serology = "1"},
                    new AlleleTestData
                        {AlleleName = "*02:01:01:01", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:UE", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:01:01:05", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:UE", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:01:01:07", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:UE", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:01:01:08", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:UE", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:01:01:18", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:UE", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:02:01:01", PGroup = "02:02P", GGroup = "02:02:01G", NmdpCode = "*02:EEE", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:02:01:02", PGroup = "02:02P", GGroup = "02:02:01G", NmdpCode = "*02:EEE", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:05:01:01", PGroup = "02:05P", GGroup = "02:05:01G", NmdpCode = "*02:GHD", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:06:01:01", PGroup = "02:06P", GGroup = "02:06:01G", NmdpCode = "*02:JFP", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:06:01:02", PGroup = "02:06P", GGroup = "02:06:01G", NmdpCode = "*02:JFP", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:06:01:03", PGroup = "02:06P", GGroup = "02:06:01G", NmdpCode = "*02:JFP", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:01", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:AG", Serology = "3"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:03", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:AG", Serology = "3"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:05", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:AG", Serology = "3"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:06", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:AG", Serology = "3"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:07", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:AG", Serology = "3"},
                    new AlleleTestData
                        {AlleleName = "*11:01:01:01", PGroup = "11:01P", GGroup = "11:01:01G", NmdpCode = "*11:BMP", Serology = "11"},
                    new AlleleTestData
                        {AlleleName = "*11:01:01:07", PGroup = "11:01P", GGroup = "11:01:01G", NmdpCode = "*11:BMP", Serology = "11"},
                    new AlleleTestData
                        {AlleleName = "*23:01:01:01", PGroup = "23:01P", GGroup = "23:01:01G", NmdpCode = "*23:AVK", Serology = "23"},
                    new AlleleTestData
                        {AlleleName = "*24:02:01:01", PGroup = "24:02P", GGroup = "24:02:01G", NmdpCode = "*24:HGF", Serology = "24"},
                    new AlleleTestData
                        {AlleleName = "*24:02:01:02L", PGroup = "24:02P", GGroup = "24:02:01G", NmdpCode = "*24:HGF", Serology = "24"},
                    new AlleleTestData
                        {AlleleName = "*24:02:01:04", PGroup = "24:02P", GGroup = "24:02:01G", NmdpCode = "*24:HGF", Serology = "24"},
                    new AlleleTestData
                        {AlleleName = "*24:02:01:05", PGroup = "24:02P", GGroup = "24:02:01G", NmdpCode = "*24:HGF", Serology = "24"},
                    new AlleleTestData
                        {AlleleName = "*24:02:01:06", PGroup = "24:02P", GGroup = "24:02:01G", NmdpCode = "*24:HGF", Serology = "24"},
                    new AlleleTestData
                        {AlleleName = "*24:02:01:07", PGroup = "24:02P", GGroup = "24:02:01G", NmdpCode = "*24:HGF", Serology = "24"},
                    new AlleleTestData
                    {
                        AlleleName = "*24:03:01:01",
                        PGroup = "24:03P",
                        GGroup = "24:03:01G",
                        NmdpCode = "*24:BCK",
                        Serology = "2403"
                    },
                    new AlleleTestData
                        {AlleleName = "*24:20:01:02", PGroup = "24:20P", GGroup = "24:20:01G", NmdpCode = "*24:BTEH", Serology = "24"},
                    new AlleleTestData
                        {AlleleName = "*25:01:01:01", PGroup = "25:01P", GGroup = "25:01:01G", NmdpCode = "*25:AB", Serology = "25"},
                    new AlleleTestData
                        {AlleleName = "*26:01:01:01", PGroup = "26:01P", GGroup = "26:01:01G", NmdpCode = "*26:HHZ", Serology = "26"},
                    new AlleleTestData
                        {AlleleName = "*26:01:01:02", PGroup = "26:01P", GGroup = "26:01:01G", NmdpCode = "*26:HHZ", Serology = "26"},
                    new AlleleTestData
                        {AlleleName = "*29:01:01:01", PGroup = "29:01P", GGroup = "29:01:01G", NmdpCode = "*29:MS", Serology = "29"},
                    new AlleleTestData
                        {AlleleName = "*29:02:01:01", PGroup = "29:02P", GGroup = "29:02:01G", NmdpCode = "*29:BC", Serology = "29"},
                    new AlleleTestData
                        {AlleleName = "*29:02:01:02", PGroup = "29:02P", GGroup = "29:02:01G", NmdpCode = "*29:BC", Serology = "29"},
                    new AlleleTestData
                        {AlleleName = "*29:02:01:03", PGroup = "29:02P", GGroup = "29:02:01G", NmdpCode = "*29:BC", Serology = "29"},
                    new AlleleTestData
                        {AlleleName = "*30:02:01:01", PGroup = "30:02P", GGroup = "30:02:01G", NmdpCode = "*30:BC", Serology = "30"},
                    new AlleleTestData
                        {AlleleName = "*30:02:01:02", PGroup = "30:02P", GGroup = "30:02:01G", NmdpCode = "*30:BC", Serology = "30"},
                    new AlleleTestData
                        {AlleleName = "*30:02:01:03", PGroup = "30:02P", GGroup = "30:02:01G", NmdpCode = "*30:BC", Serology = "30"},
                    new AlleleTestData
                        {AlleleName = "*31:01:02:01", PGroup = "31:01P", GGroup = "31:01:02G", NmdpCode = "*31:MP", Serology = "31"},
                    new AlleleTestData
                        {AlleleName = "*31:01:02:04", PGroup = "31:01P", GGroup = "31:01:02G", NmdpCode = "*31:MP", Serology = "31"},
                    new AlleleTestData
                        {AlleleName = "*32:01:01:01", PGroup = "32:01P", GGroup = "32:01:01G", NmdpCode = "*32:SY", Serology = "32"},
                    new AlleleTestData
                        {AlleleName = "*33:01:01:01", PGroup = "33:01P", GGroup = "33:01:01G", NmdpCode = "*33:DWH", Serology = "33"},
                    new AlleleTestData
                        {AlleleName = "*33:03:01:01", PGroup = "33:03P", GGroup = "33:03:01G", NmdpCode = "*33:TC", Serology = "33"},
                    new AlleleTestData
                        {AlleleName = "*66:01:01:01", PGroup = "66:01P", GGroup = "66:01:01G", NmdpCode = "*66:MS", Serology = "66"},
                    new AlleleTestData
                        {AlleleName = "*68:01:01:02", PGroup = "68:01P", GGroup = "68:01:01G", NmdpCode = "*68:AMT", Serology = "68"},
                    new AlleleTestData
                        {AlleleName = "*68:01:02:01", PGroup = "68:01P", GGroup = "68:01:02G", NmdpCode = "*68:AMT", Serology = "68"},
                    new AlleleTestData
                        {AlleleName = "*68:01:02:02", PGroup = "68:01P", GGroup = "68:01:02G", NmdpCode = "*68:AMT", Serology = "68"},
                    new AlleleTestData
                        {AlleleName = "*68:01:02:03", PGroup = "68:01P", GGroup = "68:01:02G", NmdpCode = "*68:AMT", Serology = "68"},
                    new AlleleTestData
                        {AlleleName = "*68:02:01:01", PGroup = "68:02P", GGroup = "68:02:01G", NmdpCode = "*68:XV", Serology = "68"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData
                        {AlleleName = "*01:01:01:01", PGroup = "01:01P", GGroup = "01:01:01G", NmdpCode = "*01:AG", Serology = "1"},
                    new AlleleTestData
                        {AlleleName = "*01:01:01:03", PGroup = "01:01P", GGroup = "01:01:01G", NmdpCode = "*01:AG", Serology = "1"},
                    new AlleleTestData
                        {AlleleName = "*02:01:01:01", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:UE", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:01:01:05", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:UE", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:01:01:08", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:UE", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:02:01:01", PGroup = "02:02P", GGroup = "02:02:01G", NmdpCode = "*02:EEE", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:02:01:02", PGroup = "02:02P", GGroup = "02:02:01G", NmdpCode = "*02:EEE", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:05:01:01", PGroup = "02:05P", GGroup = "02:05:01G", NmdpCode = "*02:GHD", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:06:01:01", PGroup = "02:06P", GGroup = "02:06:01G", NmdpCode = "*02:JFP", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:06:01:02", PGroup = "02:06P", GGroup = "02:06:01G", NmdpCode = "*02:JFP", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:06:01:03", PGroup = "02:06P", GGroup = "02:06:01G", NmdpCode = "*02:JFP", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:01", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:AG", Serology = "3"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:03", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:AG", Serology = "3"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:05", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:AG", Serology = "3"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:06", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:AG", Serology = "3"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:07", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:AG", Serology = "3"},
                    new AlleleTestData
                        {AlleleName = "*11:01:01:01", PGroup = "11:01P", GGroup = "11:01:01G", NmdpCode = "*11:BMP", Serology = "11"},
                    new AlleleTestData
                        {AlleleName = "*11:01:01:07", PGroup = "11:01P", GGroup = "11:01:01G", NmdpCode = "*11:BMP", Serology = "11"},
                    new AlleleTestData
                        {AlleleName = "*23:01:01:01", PGroup = "23:01P", GGroup = "23:01:01G", NmdpCode = "*23:AVK", Serology = "23"},
                    new AlleleTestData
                        {AlleleName = "*24:02:01:01", PGroup = "24:02P", GGroup = "24:02:01G", NmdpCode = "*24:HGF", Serology = "24"},
                    new AlleleTestData
                        {AlleleName = "*24:02:01:02L", PGroup = "24:02P", GGroup = "24:02:01G", NmdpCode = "*24:HGF", Serology = "24"},
                    new AlleleTestData
                        {AlleleName = "*24:02:01:04", PGroup = "24:02P", GGroup = "24:02:01G", NmdpCode = "*24:HGF", Serology = "24"},
                    new AlleleTestData
                        {AlleleName = "*24:02:01:05", PGroup = "24:02P", GGroup = "24:02:01G", NmdpCode = "*24:HGF", Serology = "24"},
                    new AlleleTestData
                    {
                        AlleleName = "*24:03:01:01",
                        PGroup = "24:03P",
                        GGroup = "24:03:01G",
                        NmdpCode = "*24:BCK",
                        Serology = "2403"
                    },
                    new AlleleTestData
                        {AlleleName = "*25:01:01:01", PGroup = "25:01P", GGroup = "25:01:01G", NmdpCode = "*25:AB", Serology = "25"},
                    new AlleleTestData
                        {AlleleName = "*26:01:01:01", PGroup = "26:01P", GGroup = "26:01:01G", NmdpCode = "*26:HHZ", Serology = "26"},
                    new AlleleTestData
                        {AlleleName = "*26:01:01:02", PGroup = "26:01P", GGroup = "26:01:01G", NmdpCode = "*26:HHZ", Serology = "26"},
                    new AlleleTestData
                        {AlleleName = "*26:01:01:05", PGroup = "26:01P", GGroup = "26:01:01G", NmdpCode = "*26:HHZ", Serology = "26"},
                    new AlleleTestData
                        {AlleleName = "*29:01:01:01", PGroup = "29:01P", GGroup = "29:01:01G", NmdpCode = "*29:MS", Serology = "29"},
                    new AlleleTestData
                        {AlleleName = "*29:02:01:01", PGroup = "29:02P", GGroup = "29:02:01G", NmdpCode = "*29:BC", Serology = "29"},
                    new AlleleTestData
                        {AlleleName = "*29:02:01:02", PGroup = "29:02P", GGroup = "29:02:01G", NmdpCode = "*29:BC", Serology = "29"},
                    new AlleleTestData
                        {AlleleName = "*29:02:01:03", PGroup = "29:02P", GGroup = "29:02:01G", NmdpCode = "*29:BC", Serology = "29"},
                    new AlleleTestData
                        {AlleleName = "*29:02:01:04", PGroup = "29:02P", GGroup = "29:02:01G", NmdpCode = "*29:BC", Serology = "29"},
                    new AlleleTestData
                        {AlleleName = "*30:02:01:01", PGroup = "30:02P", GGroup = "30:02:01G", NmdpCode = "*30:BC", Serology = "30"},
                    new AlleleTestData
                        {AlleleName = "*30:02:01:02", PGroup = "30:02P", GGroup = "30:02:01G", NmdpCode = "*30:BC", Serology = "30"},
                    new AlleleTestData
                        {AlleleName = "*30:02:01:03", PGroup = "30:02P", GGroup = "30:02:01G", NmdpCode = "*30:BC", Serology = "30"},
                    new AlleleTestData
                        {AlleleName = "*31:01:02:01", PGroup = "31:01P", GGroup = "31:01:02G", NmdpCode = "*31:MP", Serology = "31"},
                    new AlleleTestData
                        {AlleleName = "*31:01:02:02", PGroup = "31:01P", GGroup = "31:01:02G", NmdpCode = "*31:MP", Serology = "31"},
                    new AlleleTestData
                        {AlleleName = "*31:01:02:04", PGroup = "31:01P", GGroup = "31:01:02G", NmdpCode = "*31:MP", Serology = "31"},
                    new AlleleTestData
                        {AlleleName = "*32:01:01:01", PGroup = "32:01P", GGroup = "32:01:01G", NmdpCode = "*32:SY", Serology = "32"},
                    new AlleleTestData
                        {AlleleName = "*33:01:01:01", PGroup = "33:01P", GGroup = "33:01:01G", NmdpCode = "*33:DWH", Serology = "33"},
                    new AlleleTestData
                        {AlleleName = "*33:01:01:02", PGroup = "33:01P", GGroup = "33:01:01G", NmdpCode = "*33:DWH", Serology = "33"},
                    new AlleleTestData
                        {AlleleName = "*33:03:01:01", PGroup = "33:03P", GGroup = "33:03:01G", NmdpCode = "*33:TC", Serology = "33"},
                    new AlleleTestData
                        {AlleleName = "*66:01:01:01", PGroup = "66:01P", GGroup = "66:01:01G", NmdpCode = "*66:MS", Serology = "66"},
                    new AlleleTestData
                        {AlleleName = "*68:01:01:02", PGroup = "68:01P", GGroup = "68:01:01G", NmdpCode = "*68:AMT", Serology = "68"},
                    new AlleleTestData
                        {AlleleName = "*68:01:02:01", PGroup = "68:01P", GGroup = "68:01:02G", NmdpCode = "*68:AMT", Serology = "68"},
                    new AlleleTestData
                        {AlleleName = "*68:01:02:02", PGroup = "68:01P", GGroup = "68:01:02G", NmdpCode = "*68:AMT", Serology = "68"},
                    new AlleleTestData
                        {AlleleName = "*68:01:02:03", PGroup = "68:01P", GGroup = "68:01:02G", NmdpCode = "*68:AMT", Serology = "68"},
                    new AlleleTestData
                        {AlleleName = "*68:02:01:01", PGroup = "68:02P", GGroup = "68:02:01G", NmdpCode = "*68:XV", Serology = "68"},
                    new AlleleTestData
                        {AlleleName = "*68:02:01:03", PGroup = "68:02P", GGroup = "68:02:01G", NmdpCode = "*68:XV", Serology = "68"},
                    new AlleleTestData
                        {AlleleName = "*80:01:01:02", PGroup = "80:01P", GGroup = "80:01:01G", NmdpCode = "*80:AB", Serology = "80"},
                }
            ),
            valueB: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData
                        {AlleleName = "*07:02:01:01", PGroup = "07:02P", GGroup = "07:02:01G", NmdpCode = "*07:KT", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:05:01:01", PGroup = "07:05P", GGroup = "07:05:01G", NmdpCode = "*07:JNC", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*08:01:01:01", PGroup = "08:01P", GGroup = "08:01:01G", NmdpCode = "*08:AC", Serology = "8"},
                    new AlleleTestData
                        {AlleleName = "*08:01:01:02", PGroup = "08:01P", GGroup = "08:01:01G", NmdpCode = "*08:AC", Serology = "8"},
                    new AlleleTestData
                        {AlleleName = "*13:02:01:01", PGroup = "13:02P", GGroup = "13:02:01G", NmdpCode = "*13:NT", Serology = "13"},
                    new AlleleTestData
                        {AlleleName = "*14:01:01:01", PGroup = "14:01P", GGroup = "14:01:01G", NmdpCode = "*14:PT", Serology = "64"},
                    new AlleleTestData
                        {AlleleName = "*14:02:01:01", PGroup = "14:02P", GGroup = "14:02:01G", NmdpCode = "*14:BF", Serology = "65"},
                    new AlleleTestData
                        {AlleleName = "*14:02:01:02", PGroup = "14:02P", GGroup = "14:02:01G", NmdpCode = "*14:BF", Serology = "65"},
                    new AlleleTestData
                        {AlleleName = "*14:02:01:03", PGroup = "14:02P", GGroup = "14:02:01G", NmdpCode = "*14:BF", Serology = "65"},
                    new AlleleTestData
                        {AlleleName = "*15:01:01:01", PGroup = "15:01P", GGroup = "15:01:01G", NmdpCode = "*15:DPJ", Serology = "62"},
                    new AlleleTestData
                        {AlleleName = "*15:01:01:04", PGroup = "15:01P", GGroup = "15:01:01G", NmdpCode = "*15:DPJ", Serology = "62"},
                    new AlleleTestData
                        {AlleleName = "*15:01:01:06", PGroup = "15:01P", GGroup = "15:01:01G", NmdpCode = "*15:DPJ", Serology = "62"},
                    new AlleleTestData
                        {AlleleName = "*15:03:01:02", PGroup = "15:03P", GGroup = "15:03:01G", NmdpCode = "*15:CJ", Serology = "72"},
                    new AlleleTestData
                        {AlleleName = "*15:04:01:02", PGroup = "15:04P", GGroup = "15:04:01G", NmdpCode = "*15:DTU", Serology = "62"},
                    new AlleleTestData
                        {AlleleName = "*15:16:01:02", PGroup = "15:16P", GGroup = "15:16:01G", NmdpCode = "*15:DMP", Serology = "63"},
                    new AlleleTestData
                        {AlleleName = "*15:17:01:01", PGroup = "15:17P", GGroup = "15:17:01G", NmdpCode = "*15:WMK", Serology = "63"},
                    new AlleleTestData
                        {AlleleName = "*18:01:01:01", PGroup = "18:01P", GGroup = "18:01:01G", NmdpCode = "*18:FDX", Serology = "18"},
                    new AlleleTestData
                        {AlleleName = "*18:01:01:02", PGroup = "18:01P", GGroup = "18:01:01G", NmdpCode = "*18:FDX", Serology = "18"},
                    new AlleleTestData
                        {AlleleName = "*18:01:01:03", PGroup = "18:01P", GGroup = "18:01:01G", NmdpCode = "*18:FDX", Serology = "18"},
                    new AlleleTestData
                        {AlleleName = "*27:05:02:01", PGroup = "27:05P", GGroup = "27:05:02G", NmdpCode = "*27:EG", Serology = "27"},
                    new AlleleTestData
                        {AlleleName = "*35:01:01:02", PGroup = "35:01P", GGroup = "35:01:01G", NmdpCode = "*35:RV", Serology = "35"},
                    new AlleleTestData
                        {AlleleName = "*35:03:01:01", PGroup = "35:03P", GGroup = "35:03:01G", NmdpCode = "*35:ANM", Serology = "35"},
                    new AlleleTestData
                        {AlleleName = "*35:03:01:03", PGroup = "35:03P", GGroup = "35:03:01G", NmdpCode = "*35:ANM", Serology = "35"},
                    new AlleleTestData
                        {AlleleName = "*35:08:01:01", PGroup = "35:08P", GGroup = "35:08:01G", NmdpCode = "*35:BFJ", Serology = "35"},
                    new AlleleTestData
                        {AlleleName = "*37:01:01:01", PGroup = "37:01P", GGroup = "37:01:01G", NmdpCode = "*37:JTB", Serology = "37"},
                    new AlleleTestData
                        {AlleleName = "*38:01:01:01", PGroup = "38:01P", GGroup = "38:01:01G", NmdpCode = "*38:MN", Serology = "38"},
                    new AlleleTestData
                    {
                        AlleleName = "*39:01:01:01",
                        PGroup = "39:01P",
                        GGroup = "39:01:01G",
                        NmdpCode = "*39:CKD",
                        Serology = "3901"
                    },
                    new AlleleTestData
                    {
                        AlleleName = "*39:01:01:03",
                        PGroup = "39:01P",
                        GGroup = "39:01:01G",
                        NmdpCode = "*39:CKD",
                        Serology = "3901"
                    },
                    new AlleleTestData
                        {AlleleName = "*39:06:02:01", PGroup = "39:06P", GGroup = "39:06:02G", NmdpCode = "*39:GDZ", Serology = "39"},
                    new AlleleTestData
                        {AlleleName = "*40:06:01:01", PGroup = "40:06P", GGroup = "40:06:01G", NmdpCode = "*40:NZS", Serology = "61"},
                    new AlleleTestData
                        {AlleleName = "*40:06:01:02", PGroup = "40:06P", GGroup = "40:06:01G", NmdpCode = "*40:NZS", Serology = "61"},
                    new AlleleTestData
                        {AlleleName = "*44:02:01:01", PGroup = "44:02P", GGroup = "44:02:01G", NmdpCode = "*44:FGM", Serology = "44"},
                    new AlleleTestData
                        {AlleleName = "*44:02:01:03", PGroup = "44:02P", GGroup = "44:02:01G", NmdpCode = "*44:FGM", Serology = "44"},
                    new AlleleTestData
                        {AlleleName = "*44:03:01:01", PGroup = "44:03P", GGroup = "44:03:01G", NmdpCode = "*44:FGN", Serology = "44"},
                    new AlleleTestData
                        {AlleleName = "*44:03:01:02", PGroup = "44:03P", GGroup = "44:03:01G", NmdpCode = "*44:FGN", Serology = "44"},
                    new AlleleTestData
                        {AlleleName = "*44:03:01:03", PGroup = "44:03P", GGroup = "44:03:01G", NmdpCode = "*44:FGN", Serology = "44"},
                    new AlleleTestData
                        {AlleleName = "*44:03:01:09", PGroup = "44:03P", GGroup = "44:03:01G", NmdpCode = "*44:FGN", Serology = "44"},
                    new AlleleTestData
                        {AlleleName = "*47:01:01:03", PGroup = "47:01P", GGroup = "47:01:01G", NmdpCode = "*47:AD", Serology = "47"},
                    new AlleleTestData
                        {AlleleName = "*49:01:01:01", PGroup = "49:01P", GGroup = "49:01:01G", NmdpCode = "*49:AC", Serology = "49"},
                    new AlleleTestData
                        {AlleleName = "*50:01:01:01", PGroup = "50:01P", GGroup = "50:01:01G", NmdpCode = "*50:AB", Serology = "50"},
                    new AlleleTestData
                        {AlleleName = "*50:01:01:02", PGroup = "50:01P", GGroup = "50:01:01G", NmdpCode = "*50:AB", Serology = "50"},
                    new AlleleTestData
                        {AlleleName = "*51:01:01:01", PGroup = "51:01P", GGroup = "51:01:01G", NmdpCode = "*51:HJH", Serology = "51"},
                    new AlleleTestData
                        {AlleleName = "*51:01:01:14", PGroup = "51:01P", GGroup = "51:01:01G", NmdpCode = "*51:HJH", Serology = "51"},
                    new AlleleTestData
                        {AlleleName = "*51:01:01:21", PGroup = "51:01P", GGroup = "51:01:01G", NmdpCode = "*51:HJH", Serology = "51"},
                    new AlleleTestData
                        {AlleleName = "*52:01:01:01", PGroup = "52:01P", GGroup = "52:01:01G", NmdpCode = "*52:AC", Serology = "52"},
                    new AlleleTestData
                        {AlleleName = "*52:01:01:02", PGroup = "52:01P", GGroup = "52:01:01G", NmdpCode = "*52:AC", Serology = "52"},
                    new AlleleTestData
                        {AlleleName = "*55:02:01:02", PGroup = "55:02P", GGroup = "55:02:01G", NmdpCode = "*55:WM", Serology = "55"},
                    new AlleleTestData
                        {AlleleName = "*56:01:01:01", PGroup = "56:01P", GGroup = "56:01:01G", NmdpCode = "*56:AB", Serology = "56"},
                    new AlleleTestData
                        {AlleleName = "*56:01:01:02", PGroup = "56:01P", GGroup = "56:01:01G", NmdpCode = "*56:AB", Serology = "56"},
                    new AlleleTestData
                        {AlleleName = "*56:01:01:03", PGroup = "56:01P", GGroup = "56:01:01G", NmdpCode = "*56:AB", Serology = "56"},
                    new AlleleTestData
                        {AlleleName = "*56:01:01:04", PGroup = "56:01P", GGroup = "56:01:01G", NmdpCode = "*56:AB", Serology = "56"},
                    new AlleleTestData
                        {AlleleName = "*57:01:01:01", PGroup = "57:01P", GGroup = "57:01:01G", NmdpCode = "*57:SY", Serology = "57"},
                    new AlleleTestData
                        {AlleleName = "*57:03:01:01", PGroup = "57:03P", GGroup = "57:03:01G", NmdpCode = "*57:CG", Serology = "57"},
                    new AlleleTestData
                        {AlleleName = "*57:03:01:02", PGroup = "57:03P", GGroup = "57:03:01G", NmdpCode = "*57:CG", Serology = "57"},
                    new AlleleTestData
                        {AlleleName = "*58:01:01:01", PGroup = "58:01P", GGroup = "58:01:01G", NmdpCode = "*58:AD", Serology = "58"},
                    new AlleleTestData
                        {AlleleName = "*58:01:01:04", PGroup = "58:01P", GGroup = "58:01:01G", NmdpCode = "*58:AD", Serology = "58"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData
                        {AlleleName = "*07:02:01:01", PGroup = "07:02P", GGroup = "07:02:01G", NmdpCode = "*07:KT", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:05:01:01", PGroup = "07:05P", GGroup = "07:05:01G", NmdpCode = "*07:JNC", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*08:01:01:01", PGroup = "08:01P", GGroup = "08:01:01G", NmdpCode = "*08:AC", Serology = "8"},
                    new AlleleTestData
                        {AlleleName = "*08:01:01:02", PGroup = "08:01P", GGroup = "08:01:01G", NmdpCode = "*08:AC", Serology = "8"},
                    new AlleleTestData
                        {AlleleName = "*13:02:01:01", PGroup = "13:02P", GGroup = "13:02:01G", NmdpCode = "*13:NT", Serology = "13"},
                    new AlleleTestData
                        {AlleleName = "*14:01:01:01", PGroup = "14:01P", GGroup = "14:01:01G", NmdpCode = "*14:PT", Serology = "64"},
                    new AlleleTestData
                        {AlleleName = "*14:02:01:01", PGroup = "14:02P", GGroup = "14:02:01G", NmdpCode = "*14:BF", Serology = "65"},
                    new AlleleTestData
                        {AlleleName = "*14:02:01:02", PGroup = "14:02P", GGroup = "14:02:01G", NmdpCode = "*14:BF", Serology = "65"},
                    new AlleleTestData
                        {AlleleName = "*14:02:01:03", PGroup = "14:02P", GGroup = "14:02:01G", NmdpCode = "*14:BF", Serology = "65"},
                    new AlleleTestData
                        {AlleleName = "*15:01:01:01", PGroup = "15:01P", GGroup = "15:01:01G", NmdpCode = "*15:DPJ", Serology = "62"},
                    new AlleleTestData
                        {AlleleName = "*15:01:01:03", PGroup = "15:01P", GGroup = "15:01:01G", NmdpCode = "*15:DPJ", Serology = "62"},
                    new AlleleTestData
                        {AlleleName = "*15:01:01:04", PGroup = "15:01P", GGroup = "15:01:01G", NmdpCode = "*15:DPJ", Serology = "62"},
                    new AlleleTestData
                        {AlleleName = "*15:01:01:06", PGroup = "15:01P", GGroup = "15:01:01G", NmdpCode = "*15:DPJ", Serology = "62"},
                    new AlleleTestData
                        {AlleleName = "*15:01:01:09", PGroup = "15:01P", GGroup = "15:01:01G", NmdpCode = "*15:DPJ", Serology = "62"},
                    new AlleleTestData
                        {AlleleName = "*15:03:01:01", PGroup = "15:03P", GGroup = "15:03:01G", NmdpCode = "*15:CJ", Serology = "72"},
                    new AlleleTestData
                        {AlleleName = "*15:03:01:02", PGroup = "15:03P", GGroup = "15:03:01G", NmdpCode = "*15:CJ", Serology = "72"},
                    new AlleleTestData
                        {AlleleName = "*15:07:01:01", PGroup = "15:07P", GGroup = "15:07:01G", NmdpCode = "*15:CUJ", Serology = "62"},
                    new AlleleTestData
                        {AlleleName = "*15:07:01:02", PGroup = "15:07P", GGroup = "15:07:01G", NmdpCode = "*15:CUJ", Serology = "62"},
                    new AlleleTestData
                        {AlleleName = "*15:16:01:02", PGroup = "15:16P", GGroup = "15:16:01G", NmdpCode = "*15:DMP", Serology = "63"},
                    new AlleleTestData
                        {AlleleName = "*15:17:01:01", PGroup = "15:17P", GGroup = "15:17:01G", NmdpCode = "*15:WMK", Serology = "63"},
                    new AlleleTestData
                        {AlleleName = "*15:18:01:02", PGroup = "15:18P", GGroup = "15:18:01G", NmdpCode = "*15:DNG", Serology = "71"},
                    new AlleleTestData
                        {AlleleName = "*15:18:01:03", PGroup = "15:18P", GGroup = "15:18:01G", NmdpCode = "*15:DNG", Serology = "71"},
                    new AlleleTestData
                        {AlleleName = "*18:01:01:01", PGroup = "18:01P", GGroup = "18:01:01G", NmdpCode = "*18:FDX", Serology = "18"},
                    new AlleleTestData
                        {AlleleName = "*18:01:01:02", PGroup = "18:01P", GGroup = "18:01:01G", NmdpCode = "*18:FDX", Serology = "18"},
                    new AlleleTestData
                        {AlleleName = "*18:01:01:03", PGroup = "18:01P", GGroup = "18:01:01G", NmdpCode = "*18:FDX", Serology = "18"},
                    new AlleleTestData
                        {AlleleName = "*27:02:01:04", PGroup = "27:02P", GGroup = "27:02:01G", NmdpCode = "*27:ARM", Serology = "27"},
                    new AlleleTestData
                        {AlleleName = "*27:05:02:01", PGroup = "27:05P", GGroup = "27:05:02G", NmdpCode = "*27:EG", Serology = "27"},
                    new AlleleTestData
                        {AlleleName = "*35:01:01:02", PGroup = "35:01P", GGroup = "35:01:01G", NmdpCode = "*35:RV", Serology = "35"},
                    new AlleleTestData
                        {AlleleName = "*35:03:01:01", PGroup = "35:03P", GGroup = "35:03:01G", NmdpCode = "*35:ANM", Serology = "35"},
                    new AlleleTestData
                        {AlleleName = "*35:03:01:03", PGroup = "35:03P", GGroup = "35:03:01G", NmdpCode = "*35:ANM", Serology = "35"},
                    new AlleleTestData
                        {AlleleName = "*35:05:01:01", PGroup = "35:05P", GGroup = "35:05:01G", NmdpCode = "*35:EME", Serology = "35"},
                    new AlleleTestData
                        {AlleleName = "*35:08:01:01", PGroup = "35:08P", GGroup = "35:08:01G", NmdpCode = "*35:BFJ", Serology = "35"},
                    new AlleleTestData
                        {AlleleName = "*37:01:01:01", PGroup = "37:01P", GGroup = "37:01:01G", NmdpCode = "*37:JTB", Serology = "37"},
                    new AlleleTestData
                        {AlleleName = "*38:01:01:01", PGroup = "38:01P", GGroup = "38:01:01G", NmdpCode = "*38:MN", Serology = "38"},
                    new AlleleTestData
                    {
                        AlleleName = "*39:01:01:01",
                        PGroup = "39:01P",
                        GGroup = "39:01:01G",
                        NmdpCode = "*39:CKD",
                        Serology = "3901"
                    },
                    new AlleleTestData
                    {
                        AlleleName = "*39:01:01:03",
                        PGroup = "39:01P",
                        GGroup = "39:01:01G",
                        NmdpCode = "*39:CKD",
                        Serology = "3901"
                    },
                    new AlleleTestData
                        {AlleleName = "*39:06:02:01", PGroup = "39:06P", GGroup = "39:06:02G", NmdpCode = "*39:GDZ", Serology = "39"},
                    new AlleleTestData
                        {AlleleName = "*40:02:01:01", PGroup = "40:02P", GGroup = "40:02:01G", NmdpCode = "*40:CXS", Serology = "61"},
                    new AlleleTestData
                        {AlleleName = "*40:06:01:01", PGroup = "40:06P", GGroup = "40:06:01G", NmdpCode = "*40:NZS", Serology = "61"},
                    new AlleleTestData
                        {AlleleName = "*40:06:01:02", PGroup = "40:06P", GGroup = "40:06:01G", NmdpCode = "*40:NZS", Serology = "61"},
                    new AlleleTestData
                        {AlleleName = "*40:06:04:01", PGroup = "40:06P", GGroup = "40:06:01G", NmdpCode = "*40:NZS", Serology = "61"},
                    new AlleleTestData
                        {AlleleName = "*42:02:01:02", PGroup = "42:02P", GGroup = "42:02:01G", NmdpCode = "*42:BH", Serology = "42"},
                    new AlleleTestData
                        {AlleleName = "*44:02:01:01", PGroup = "44:02P", GGroup = "44:02:01G", NmdpCode = "*44:FGM", Serology = "44"},
                    new AlleleTestData
                        {AlleleName = "*44:02:01:03", PGroup = "44:02P", GGroup = "44:02:01G", NmdpCode = "*44:FGM", Serology = "44"},
                    new AlleleTestData
                        {AlleleName = "*44:03:01:01", PGroup = "44:03P", GGroup = "44:03:01G", NmdpCode = "*44:FGN", Serology = "44"},
                    new AlleleTestData
                        {AlleleName = "*44:03:01:02", PGroup = "44:03P", GGroup = "44:03:01G", NmdpCode = "*44:FGN", Serology = "44"},
                    new AlleleTestData
                        {AlleleName = "*44:03:01:03", PGroup = "44:03P", GGroup = "44:03:01G", NmdpCode = "*44:FGN", Serology = "44"},
                    new AlleleTestData
                        {AlleleName = "*47:01:01:03", PGroup = "47:01P", GGroup = "47:01:01G", NmdpCode = "*47:AD", Serology = "47"},
                    new AlleleTestData
                        {AlleleName = "*49:01:01:01", PGroup = "49:01P", GGroup = "49:01:01G", NmdpCode = "*49:AC", Serology = "49"},
                    new AlleleTestData
                        {AlleleName = "*49:01:01:02", PGroup = "49:01P", GGroup = "49:01:01G", NmdpCode = "*49:AC", Serology = "49"},
                    new AlleleTestData
                        {AlleleName = "*50:01:01:01", PGroup = "50:01P", GGroup = "50:01:01G", NmdpCode = "*50:AB", Serology = "50"},
                    new AlleleTestData
                        {AlleleName = "*50:01:01:02", PGroup = "50:01P", GGroup = "50:01:01G", NmdpCode = "*50:AB", Serology = "50"},
                    new AlleleTestData
                        {AlleleName = "*51:01:01:01", PGroup = "51:01P", GGroup = "51:01:01G", NmdpCode = "*51:HJH", Serology = "51"},
                    new AlleleTestData
                        {AlleleName = "*52:01:01:01", PGroup = "52:01P", GGroup = "52:01:01G", NmdpCode = "*52:AC", Serology = "52"},
                    new AlleleTestData
                        {AlleleName = "*52:01:01:02", PGroup = "52:01P", GGroup = "52:01:01G", NmdpCode = "*52:AC", Serology = "52"},
                    new AlleleTestData
                        {AlleleName = "*55:02:01:02", PGroup = "55:02P", GGroup = "55:02:01G", NmdpCode = "*55:WM", Serology = "55"},
                    new AlleleTestData
                        {AlleleName = "*56:01:01:02", PGroup = "56:01P", GGroup = "56:01:01G", NmdpCode = "*56:AB", Serology = "56"},
                    new AlleleTestData
                        {AlleleName = "*56:01:01:03", PGroup = "56:01P", GGroup = "56:01:01G", NmdpCode = "*56:AB", Serology = "56"},
                    new AlleleTestData
                        {AlleleName = "*56:01:01:04", PGroup = "56:01P", GGroup = "56:01:01G", NmdpCode = "*56:AB", Serology = "56"},
                    new AlleleTestData
                        {AlleleName = "*57:01:01:01", PGroup = "57:01P", GGroup = "57:01:01G", NmdpCode = "*57:SY", Serology = "57"},
                    new AlleleTestData
                        {AlleleName = "*57:03:01:01", PGroup = "57:03P", GGroup = "57:03:01G", NmdpCode = "*57:CG", Serology = "57"},
                    new AlleleTestData
                        {AlleleName = "*57:03:01:02", PGroup = "57:03P", GGroup = "57:03:01G", NmdpCode = "*57:CG", Serology = "57"},
                    new AlleleTestData
                        {AlleleName = "*58:01:01:01", PGroup = "58:01P", GGroup = "58:01:01G", NmdpCode = "*58:AD", Serology = "58"},
                    new AlleleTestData
                        {AlleleName = "*58:01:01:04", PGroup = "58:01P", GGroup = "58:01:01G", NmdpCode = "*58:AD", Serology = "58"},
                    new AlleleTestData
                        {AlleleName = "*59:01:01:02", PGroup = "59:01P", GGroup = "59:01:01G", NmdpCode = "*59:AE", Serology = "59"},
                }
            ),
            valueC: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData
                        {AlleleName = "*01:02:01:01", PGroup = "01:02P", GGroup = "01:02:01G", NmdpCode = "*01:WC", Serology = "1"},
                    new AlleleTestData
                        {AlleleName = "*01:02:01:03", PGroup = "01:02P", GGroup = "01:02:01G", NmdpCode = "*01:WC", Serology = "1"},
                    new AlleleTestData
                        {AlleleName = "*02:02:02:01", PGroup = "02:02P", GGroup = "02:02:02G", NmdpCode = "*02:NT", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:02:02:02", PGroup = "02:02P", GGroup = "02:02:02G", NmdpCode = "*02:NT", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:02:02:03", PGroup = "02:02P", GGroup = "02:02:02G", NmdpCode = "*02:NT", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:10:01:01", PGroup = "02:02P", GGroup = "02:10:01G", NmdpCode = "*02:VYJ", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:10:01:02", PGroup = "02:02P", GGroup = "02:10:01G", NmdpCode = "*02:VYJ", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*03:02:02:01", PGroup = "03:02P", GGroup = "03:02:01G", NmdpCode = "*03:BF", Serology = "10"},
                    new AlleleTestData
                        {AlleleName = "*03:02:02:02", PGroup = "03:02P", GGroup = "03:02:01G", NmdpCode = "*03:BF", Serology = "10"},
                    new AlleleTestData
                        {AlleleName = "*03:03:01:01", PGroup = "03:03P", GGroup = "03:03:01G", NmdpCode = "*03:CA", Serology = "9"},
                    new AlleleTestData
                        {AlleleName = "*03:04:01:01", PGroup = "03:04P", GGroup = "03:04:01G", NmdpCode = "*03:HCD", Serology = "10"},
                    new AlleleTestData
                        {AlleleName = "*03:04:01:02", PGroup = "03:04P", GGroup = "03:04:01G", NmdpCode = "*03:HCD", Serology = "10"},
                    new AlleleTestData
                        {AlleleName = "*04:01:01:01", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:GC", Serology = "4"},
                    new AlleleTestData
                        {AlleleName = "*04:01:01:05", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:GC", Serology = "4"},
                    new AlleleTestData
                        {AlleleName = "*04:01:01:06", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:GC", Serology = "4"},
                    new AlleleTestData
                        {AlleleName = "*04:01:01:10", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:GC", Serology = "4"},
                    new AlleleTestData
                        {AlleleName = "*04:01:01:12", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:GC", Serology = "4"},
                    new AlleleTestData
                        {AlleleName = "*04:01:01:15", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:GC", Serology = "4"},
                    new AlleleTestData
                        {AlleleName = "*05:01:01:01", PGroup = "05:01P", GGroup = "05:01:01G", NmdpCode = "*05:ARSE", Serology = "5"},
                    new AlleleTestData
                        {AlleleName = "*05:01:01:02", PGroup = "05:01P", GGroup = "05:01:01G", NmdpCode = "*05:ARSE", Serology = "5"},
                    new AlleleTestData
                        {AlleleName = "*05:01:01:05", PGroup = "05:01P", GGroup = "05:01:01G", NmdpCode = "*05:ARSE", Serology = "5"},
                    new AlleleTestData
                        {AlleleName = "*05:01:01:08", PGroup = "05:01P", GGroup = "05:01:01G", NmdpCode = "*05:ARSE", Serology = "5"},
                    new AlleleTestData
                        {AlleleName = "*06:02:01:01", PGroup = "06:02P", GGroup = "06:02:01G", NmdpCode = "*06:CCW", Serology = "6"},
                    new AlleleTestData
                        {AlleleName = "*06:02:01:02", PGroup = "06:02P", GGroup = "06:02:01G", NmdpCode = "*06:CCW", Serology = "6"},
                    new AlleleTestData
                        {AlleleName = "*06:02:01:03", PGroup = "06:02P", GGroup = "06:02:01G", NmdpCode = "*06:CCW", Serology = "6"},
                    new AlleleTestData
                        {AlleleName = "*07:01:01:01", PGroup = "07:01P", GGroup = "07:01:01G", NmdpCode = "*07:CZB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:01:01:04", PGroup = "07:01P", GGroup = "07:01:01G", NmdpCode = "*07:CZB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:01:01:09", PGroup = "07:01P", GGroup = "07:01:01G", NmdpCode = "*07:CZB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:01:01:15", PGroup = "07:01P", GGroup = "07:01:01G", NmdpCode = "*07:CZB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:02:01:01", PGroup = "07:02P", GGroup = "07:02:01G", NmdpCode = "*07:FJF", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:02:01:03", PGroup = "07:02P", GGroup = "07:02:01G", NmdpCode = "*07:FJF", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:04:01:01", PGroup = "07:04P", GGroup = "07:04:01G", NmdpCode = "*07:ET", Serology = "7"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData
                        {AlleleName = "*01:02:01:01", PGroup = "01:02P", GGroup = "01:02:01G", NmdpCode = "*01:WC", Serology = "1"},
                    new AlleleTestData
                        {AlleleName = "*02:02:02:01", PGroup = "02:02P", GGroup = "02:02:02G", NmdpCode = "*02:NT", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:02:02:02", PGroup = "02:02P", GGroup = "02:02:02G", NmdpCode = "*02:NT", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:10:01:01", PGroup = "02:02P", GGroup = "02:10:01G", NmdpCode = "*02:VYJ", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*03:02:02:01", PGroup = "03:02P", GGroup = "03:02:01G", NmdpCode = "*03:BF", Serology = "10"},
                    new AlleleTestData
                        {AlleleName = "*03:02:02:02", PGroup = "03:02P", GGroup = "03:02:01G", NmdpCode = "*03:BF", Serology = "10"},
                    new AlleleTestData
                        {AlleleName = "*03:03:01:01", PGroup = "03:03P", GGroup = "03:03:01G", NmdpCode = "*03:CA", Serology = "9"},
                    new AlleleTestData
                        {AlleleName = "*03:04:01:01", PGroup = "03:04P", GGroup = "03:04:01G", NmdpCode = "*03:HCD", Serology = "10"},
                    new AlleleTestData
                        {AlleleName = "*03:04:01:02", PGroup = "03:04P", GGroup = "03:04:01G", NmdpCode = "*03:HCD", Serology = "10"},
                    new AlleleTestData
                        {AlleleName = "*04:01:01:01", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:GC", Serology = "4"},
                    new AlleleTestData
                        {AlleleName = "*04:01:01:05", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:GC", Serology = "4"},
                    new AlleleTestData
                        {AlleleName = "*04:01:01:06", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:GC", Serology = "4"},
                    new AlleleTestData
                        {AlleleName = "*04:01:01:10", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:GC", Serology = "4"},
                    new AlleleTestData
                        {AlleleName = "*05:01:01:01", PGroup = "05:01P", GGroup = "05:01:01G", NmdpCode = "*05:ARSE", Serology = "5"},
                    new AlleleTestData
                        {AlleleName = "*05:01:01:02", PGroup = "05:01P", GGroup = "05:01:01G", NmdpCode = "*05:ARSE", Serology = "5"},
                    new AlleleTestData
                        {AlleleName = "*05:01:01:03", PGroup = "05:01P", GGroup = "05:01:01G", NmdpCode = "*05:ARSE", Serology = "5"},
                    new AlleleTestData
                        {AlleleName = "*06:02:01:01", PGroup = "06:02P", GGroup = "06:02:01G", NmdpCode = "*06:CCW", Serology = "6"},
                    new AlleleTestData
                        {AlleleName = "*06:02:01:02", PGroup = "06:02P", GGroup = "06:02:01G", NmdpCode = "*06:CCW", Serology = "6"},
                    new AlleleTestData
                        {AlleleName = "*06:02:01:03", PGroup = "06:02P", GGroup = "06:02:01G", NmdpCode = "*06:CCW", Serology = "6"},
                    new AlleleTestData
                        {AlleleName = "*07:01:01:01", PGroup = "07:01P", GGroup = "07:01:01G", NmdpCode = "*07:CZB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:01:01:02", PGroup = "07:01P", GGroup = "07:01:01G", NmdpCode = "*07:CZB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:01:01:04", PGroup = "07:01P", GGroup = "07:01:01G", NmdpCode = "*07:CZB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:01:01:06", PGroup = "07:01P", GGroup = "07:01:01G", NmdpCode = "*07:CZB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:01:01:09", PGroup = "07:01P", GGroup = "07:01:01G", NmdpCode = "*07:CZB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:01:01:15", PGroup = "07:01P", GGroup = "07:01:01G", NmdpCode = "*07:CZB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:02:01:01", PGroup = "07:02P", GGroup = "07:02:01G", NmdpCode = "*07:FJF", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:02:01:03", PGroup = "07:02P", GGroup = "07:02:01G", NmdpCode = "*07:FJF", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:02:01:10", PGroup = "07:02P", GGroup = "07:02:01G", NmdpCode = "*07:FJF", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:04:01:01", PGroup = "07:04P", GGroup = "07:04:01G", NmdpCode = "*07:ET", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*07:06:01:01", PGroup = "07:01P", GGroup = "07:01:01G", NmdpCode = "*07:BBTA", Serology = "7"},
                }
            ),
            // Note that none of the DPB1 TGS 4-field alleles have a serology association.
            // DPB1 cannot be tested at this resolution
            valueDpb1: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData
                        {AlleleName = "*01:01:01:04", PGroup = "01:01P", GGroup = "01:01:01G", NmdpCode = "*01:CXC"},
                    new AlleleTestData
                        {AlleleName = "*02:02:01:01", PGroup = "02:02P", GGroup = "02:02:01G", NmdpCode = "*02:FNVN"},
                    new AlleleTestData {AlleleName = "*04:01:01:01", PGroup = "04:01P", GGroup = "03:01:01G", NmdpCode = "*04:AB"},
                    new AlleleTestData {AlleleName = "*04:01:01:02", PGroup = "04:01P", GGroup = "03:01:01G", NmdpCode = "*04:AB"},
                    new AlleleTestData {AlleleName = "*04:01:01:03", PGroup = "04:01P", GGroup = "03:01:01G", NmdpCode = "*04:AB"},
                    new AlleleTestData {AlleleName = "*04:01:01:04", PGroup = "04:01P", GGroup = "03:01:01G", NmdpCode = "*04:AB"},
                    new AlleleTestData {AlleleName = "*04:01:01:05", PGroup = "04:01P", GGroup = "03:01:01G", NmdpCode = "*04:AB"},
                    new AlleleTestData {AlleleName = "*04:01:01:06", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:AB"},
                    new AlleleTestData
                        {AlleleName = "*04:02:01:01", PGroup = "04:02P", GGroup = "04:02:01G", NmdpCode = "*04:CXF"},
                    new AlleleTestData
                        {AlleleName = "*04:02:01:02", PGroup = "04:02P", GGroup = "04:02:01G", NmdpCode = "*04:CXF"},
                    new AlleleTestData
                        {AlleleName = "*04:02:01:04", PGroup = "04:02P", GGroup = "04:02:01G", NmdpCode = "*04:CXF"},
                    new AlleleTestData {AlleleName = "*05:01:01:03", PGroup = "05:01P", GGroup = "05:01:01G", NmdpCode = "*05:SP"},
                    new AlleleTestData
                        {AlleleName = "*10:01:01:01", PGroup = "10:01P", GGroup = "10:01:01G", NmdpCode = "*10:WAW"},
                    new AlleleTestData
                        {AlleleName = "*105:01:01:01", PGroup = "04:02P", GGroup = "04:02:01G", NmdpCode = "*105:FNVS"},
                    new AlleleTestData
                        {AlleleName = "*105:01:01:03", PGroup = "04:02P", GGroup = "04:02:01G", NmdpCode = "*105:FNVS"},
                    new AlleleTestData
                        {AlleleName = "*17:01:01:01", PGroup = "17:01P", GGroup = "17:01:01G", NmdpCode = "*17:ANZX"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData
                        {AlleleName = "*02:02:01:01", PGroup = "02:02P", GGroup = "02:02:01G", NmdpCode = "*02:FNVN"},
                    new AlleleTestData {AlleleName = "*04:01:01:01", PGroup = "04:01P", GGroup = "03:01:01G", NmdpCode = "*04:AB"},
                    new AlleleTestData {AlleleName = "*04:01:01:02", PGroup = "04:01P", GGroup = "03:01:01G", NmdpCode = "*04:AB"},
                    new AlleleTestData {AlleleName = "*04:01:01:03", PGroup = "04:01P", GGroup = "03:01:01G", NmdpCode = "*04:AB"},
                    new AlleleTestData {AlleleName = "*04:01:01:04", PGroup = "04:01P", GGroup = "03:01:01G", NmdpCode = "*04:AB"},
                    new AlleleTestData {AlleleName = "*04:01:01:05", PGroup = "04:01P", GGroup = "03:01:01G", NmdpCode = "*04:AB"},
                    new AlleleTestData {AlleleName = "*04:01:01:06", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:AB"},
                    new AlleleTestData
                        {AlleleName = "*04:02:01:01", PGroup = "04:02P", GGroup = "04:02:01G", NmdpCode = "*04:CXF"},
                    new AlleleTestData
                        {AlleleName = "*04:02:01:02", PGroup = "04:02P", GGroup = "04:02:01G", NmdpCode = "*04:CXF"},
                    new AlleleTestData
                        {AlleleName = "*04:02:01:04", PGroup = "04:02P", GGroup = "04:02:01G", NmdpCode = "*04:CXF"},
                    new AlleleTestData
                        {AlleleName = "*10:01:01:01", PGroup = "10:01P", GGroup = "10:01:01G", NmdpCode = "*10:WAW"},
                    new AlleleTestData
                        {AlleleName = "*104:01:01:03", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*104:FNVX"},
                    new AlleleTestData
                        {AlleleName = "*105:01:01:01", PGroup = "04:02P", GGroup = "04:02:01G", NmdpCode = "*105:FNVS"},
                    new AlleleTestData
                        {AlleleName = "*105:01:01:02", PGroup = "04:02P", GGroup = "04:02:01G", NmdpCode = "*105:FNVS"},
                    new AlleleTestData
                        {AlleleName = "*17:01:01:01", PGroup = "17:01P", GGroup = "17:01:01G", NmdpCode = "*17:ANZX"},
                }
            ),
            valueDqb1: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData
                        {AlleleName = "*02:02:01:01", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:BC", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:02:01:02", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:BC", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:01", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:HGB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:02", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:HGB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:03", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:HGB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:04", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:HGB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:05", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:HGB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*03:02:01:01", PGroup = "03:02P", GGroup = "03:02:01G", NmdpCode = "*03:CAK", Serology = "8"},
                    new AlleleTestData
                        {AlleleName = "*03:02:01:02", PGroup = "03:02P", GGroup = "03:02:01G", NmdpCode = "*03:CAK", Serology = "8"},
                    new AlleleTestData
                        {AlleleName = "*03:03:02:01", PGroup = "03:03P", GGroup = "03:03:02G", NmdpCode = "*03:CD", Serology = "9"},
                    new AlleleTestData
                        {AlleleName = "*06:02:01:01", PGroup = "06:02P", GGroup = "06:02:01G", NmdpCode = "*06:CWV", Serology = "6"},
                    new AlleleTestData
                        {AlleleName = "*06:02:01:03", PGroup = "06:02P", GGroup = "06:02:01G", NmdpCode = "*06:CWV", Serology = "6"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData
                        {AlleleName = "*02:02:01:01", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:BC", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*02:02:01:02", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:BC", Serology = "2"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:01", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:HGB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:02", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:HGB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:03", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:HGB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:04", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:HGB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:05", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:HGB", Serology = "7"},
                    new AlleleTestData
                        {AlleleName = "*03:02:01:01", PGroup = "03:02P", GGroup = "03:02:01G", NmdpCode = "*03:CAK", Serology = "8"},
                    new AlleleTestData
                        {AlleleName = "*03:02:01:02", PGroup = "03:02P", GGroup = "03:02:01G", NmdpCode = "*03:CAK", Serology = "8"},
                    new AlleleTestData
                        {AlleleName = "*03:03:02:01", PGroup = "03:03P", GGroup = "03:03:02G", NmdpCode = "*03:CD", Serology = "9"},
                    new AlleleTestData
                        {AlleleName = "*04:02:01:01", PGroup = "04:02P", GGroup = "04:02:01G", NmdpCode = "*04:ARB", Serology = "4"},
                    new AlleleTestData
                        {AlleleName = "*06:02:01:01", PGroup = "06:02P", GGroup = "06:02:01G", NmdpCode = "*06:CWV", Serology = "6"},
                }
            ),
            valueDrb1: new LocusInfo<List<AlleleTestData>>
            (
                new List<AlleleTestData>
                {
                    new AlleleTestData
                        {AlleleName = "*03:01:01:01", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:VG", Serology = "17"},
                    new AlleleTestData
                        {AlleleName = "*03:01:01:02", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:VG", Serology = "17"},
                    new AlleleTestData
                        {AlleleName = "*04:01:01:01", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:AFP", Serology = "4"},
                    new AlleleTestData
                        {AlleleName = "*10:01:01:01", PGroup = "10:01P", GGroup = "10:01:01G", NmdpCode = "*10:AB", Serology = "10"},
                    new AlleleTestData
                        {AlleleName = "*11:01:01:01", PGroup = "11:01P", GGroup = "11:01:01G", NmdpCode = "*11:AHU", Serology = "11"},
                    new AlleleTestData
                        {AlleleName = "*13:01:01:01", PGroup = "13:01P", GGroup = "13:01:01G", NmdpCode = "*13:EV", Serology = "13"},
                    new AlleleTestData
                        {AlleleName = "*13:01:01:02", PGroup = "13:01P", GGroup = "13:01:01G", NmdpCode = "*13:EV", Serology = "13"},
                    new AlleleTestData
                        {AlleleName = "*15:01:01:01", PGroup = "15:01P", GGroup = "15:01:01G", NmdpCode = "*15:ADF", Serology = "15"},
                    new AlleleTestData
                        {AlleleName = "*15:01:01:02", PGroup = "15:01P", GGroup = "15:01:01G", NmdpCode = "*15:ADF", Serology = "15"},
                    new AlleleTestData
                        {AlleleName = "*15:02:01:01", PGroup = "15:02P", GGroup = "15:02:01G", NmdpCode = "*15:ADF", Serology = "15"},
                    new AlleleTestData
                        {AlleleName = "*15:03:01:01", PGroup = "15:03P", GGroup = "15:03:01G", NmdpCode = "*15:ADF", Serology = "15"},
                    new AlleleTestData
                        {AlleleName = "*15:03:01:02", PGroup = "15:03P", GGroup = "15:03:01G", NmdpCode = "*15:ADF", Serology = "15"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*03:01:01:01", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:VG", Serology = "17"},
                    new AlleleTestData {AlleleName = "*03:01:01:02", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:VG", Serology = "17"},
                    new AlleleTestData {AlleleName = "*07:01:01:03", PGroup = "07:01P", GGroup = "07:01:01G", NmdpCode = "*07:AE", Serology = "7"},
                    new AlleleTestData {AlleleName = "*10:01:01:01", PGroup = "10:01P", GGroup = "10:01:01G", NmdpCode = "*10:AB", Serology = "10"},
                    new AlleleTestData {AlleleName = "*11:01:01:01", PGroup = "11:01P", GGroup = "11:01:01G", NmdpCode = "*11:AHU", Serology = "11"},
                    new AlleleTestData {AlleleName = "*13:01:01:01", PGroup = "13:01P", GGroup = "13:01:01G", NmdpCode = "*13:EV", Serology = "13"},
                    new AlleleTestData {AlleleName = "*13:01:01:02", PGroup = "13:01P", GGroup = "13:01:01G", NmdpCode = "*13:EV", Serology = "13"},
                    new AlleleTestData {AlleleName = "*15:01:01:01", PGroup = "15:01P", GGroup = "15:01:01G", NmdpCode = "*15:ADF", Serology = "15"},
                    new AlleleTestData {AlleleName = "*15:01:01:02", PGroup = "15:01P", GGroup = "15:01:01G", NmdpCode = "*15:ADF", Serology = "15"},
                    new AlleleTestData {AlleleName = "*15:02:01:01", PGroup = "15:02P", GGroup = "15:02:01G", NmdpCode = "*15:ADF", Serology = "15"},
                    new AlleleTestData {AlleleName = "*15:03:01:01", PGroup = "15:03P", GGroup = "15:03:01G", NmdpCode = "*15:ADF", Serology = "15"},
                    new AlleleTestData {AlleleName = "*15:03:01:02", PGroup = "15:03P", GGroup = "15:03:01G", NmdpCode = "*15:ADF", Serology = "15"},
                }
            )
        );
    }
}