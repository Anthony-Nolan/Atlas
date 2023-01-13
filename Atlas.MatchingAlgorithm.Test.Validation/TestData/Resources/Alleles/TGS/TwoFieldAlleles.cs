using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.Alleles.TGS
{
    /// <summary>
    /// This test data was manually curated from 2-field TGS typed alleles found in the SOLAR database
    /// A corresponding NMDP code was selected for each allele from the DR_ANTIGENS table
    /// (most alleles will correspond to multiple NMDP codes - only one is necessary for testing purposes)
    /// The corresponding serology, p-group, and g-group data was retrieved from the wmda files: hla_nom_g, hla_nom_p, rel_dna_ser (v3330)
    ///
    /// This dataset may be amended, provided all data:
    /// (1) Is a 2-field, TGS typed allele
    /// (2) Has the correct p-group, g-group, and serology associations
    /// (3) Has a valid corresponding nmdp code
    /// </summary>
    public static class TwoFieldAlleles
    {
        public static readonly PhenotypeInfo<List<AlleleTestData>> Alleles = new PhenotypeInfo<List<AlleleTestData>>
        (
            valueA: new LocusInfo<List<AlleleTestData>>(
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*01:109", PGroup = "01:01P", GGroup = "01:01:01G", NmdpCode = "*01:ABTHU", Serology = "1"},
                    new AlleleTestData {AlleleName = "*01:37", PGroup = "01:01P", GGroup = "01:01:01G", NmdpCode = "*01:HWXW", Serology = "1"},
                    new AlleleTestData {AlleleName = "*01:45", PGroup = "01:01P", GGroup = "01:01:01G", NmdpCode = "*01:HWXW", Serology = "1"},
                    new AlleleTestData {AlleleName = "*02:09", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:EJHG", Serology = "2"},
                    new AlleleTestData {AlleleName = "*02:66", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:EJHG", Serology = "2"},
                    new AlleleTestData {AlleleName = "*03:112", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:KEGR", Serology = "3"},
                    new AlleleTestData {AlleleName = "*11:03", PGroup = "11:03P", GGroup = "11:03:01G", NmdpCode = "*11:DDUF", Serology = "11"},
                    new AlleleTestData {AlleleName = "*23:17", PGroup = "23:01P", GGroup = "23:01:01G", NmdpCode = "*23:EWFR", Serology = "23"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*02:04", PGroup = "02:04P", GGroup = "02:04:01G", NmdpCode = "*02:EEF", Serology = "2"},
                    new AlleleTestData {AlleleName = "*02:09", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:EJHG", Serology = "2"},
                    new AlleleTestData {AlleleName = "*02:10", PGroup = "02:10P", GGroup = "02:10:01G", NmdpCode = "*02:PYD", Serology = "210"},
                    new AlleleTestData {AlleleName = "*02:66", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:EJHG", Serology = "2"},
                    new AlleleTestData {AlleleName = "*11:03", PGroup = "11:03P", GGroup = "11:03:01G", NmdpCode = "*11:DDUF", Serology = "11"},
                    new AlleleTestData {AlleleName = "*23:17", PGroup = "23:01P", GGroup = "23:01:01G", NmdpCode = "*23:EWFR", Serology = "23"},
                    new AlleleTestData {AlleleName = "*23:18", PGroup = "23:01P", GGroup = "23:01:01G", NmdpCode = "*23:EWFR", Serology = "23"},
                }
            ),
            valueB: new LocusInfo<List<AlleleTestData>>(
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*08:182", PGroup = "08:01P", GGroup = "08:01:01G", NmdpCode = "*08:AXHCG", Serology = "8"},
                    new AlleleTestData {AlleleName = "*15:12", PGroup = "15:12P", GGroup = "15:12:01G", NmdpCode = "*15:AND", Serology = "76"},
                    new AlleleTestData {AlleleName = "*15:146", PGroup = "15:01P", GGroup = "15:01:01G", NmdpCode = "*15:GUAD", Serology = "62"},
                    new AlleleTestData {AlleleName = "*15:19", PGroup = "15:12P", GGroup = "15:12:01G", NmdpCode = "*15:WMK", Serology = "76"},
                    new AlleleTestData {AlleleName = "*15:228", PGroup = "15:01P", GGroup = "15:01:01G", NmdpCode = "*15:RZJS", Serology = "15"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*08:182", PGroup = "08:01P", GGroup = "08:01:01G", NmdpCode = "*08:AXHCG", Serology = "8"},
                    new AlleleTestData {AlleleName = "*15:146", PGroup = "15:01P", GGroup = "15:01:01G", NmdpCode = "*15:GUAD", Serology = "62"},
                    new AlleleTestData {AlleleName = "*15:228", PGroup = "15:01P", GGroup = "15:01:01G", NmdpCode = "*15:RZJS", Serology = "15"},
                }
            ),
            valueC: new LocusInfo<List<AlleleTestData>>(
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*01:03", PGroup = "01:03P", GGroup = "01:03:01G", NmdpCode = "*01:AHC", Serology = "1"},
                    new AlleleTestData {AlleleName = "*01:44", PGroup = "01:02P", GGroup = "01:02:01G", NmdpCode = "*01:AWTXA", Serology = "1"},
                    new AlleleTestData {AlleleName = "*03:05", PGroup = "03:05P", GGroup = "03:05:01G", NmdpCode = "*03:EJ", Serology = "3"},
                    new AlleleTestData {AlleleName = "*03:14", PGroup = "03:14P", GGroup = "03:14:01G", NmdpCode = "*03:EEJ", Serology = "3"},
                    new AlleleTestData {AlleleName = "*04:82", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:NYYT", Serology = "4"},
                    new AlleleTestData {AlleleName = "*05:53", PGroup = "05:01P", GGroup = "05:01:01G", NmdpCode = "*05:RJFW", Serology = "5"},
                    new AlleleTestData {AlleleName = "*07:18", PGroup = "07:01P", GGroup = "07:01:01G", NmdpCode = "*07:BBTA", Serology = "7"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*03:05", PGroup = "03:05P", GGroup = "03:05:01G", NmdpCode = "*03:EJ", Serology = "3"},
                    new AlleleTestData {AlleleName = "*03:14", PGroup = "03:14P", GGroup = "03:14:01G", NmdpCode = "*03:EEJ", Serology = "3"},
                    new AlleleTestData {AlleleName = "*04:82", PGroup = "04:01P", GGroup = "04:01:01G", NmdpCode = "*04:NYYT", Serology = "4"},
                    new AlleleTestData {AlleleName = "*07:18", PGroup = "07:01P", GGroup = "07:01:01G", NmdpCode = "*07:BBTA", Serology = "7"},
                }
            ),
            // Note that none of the DPB1 TGS 3-field alleles have a serology association.
            // DPB1 cannot be tested at this resolution
            valueDpb1: new LocusInfo<List<AlleleTestData>>(
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*01:01", NmdpCode = "*01:CXC"},
                    new AlleleTestData {AlleleName = "*09:01", NmdpCode = "*09:ANZW"},
                    new AlleleTestData {AlleleName = "*104:01", NmdpCode = "*104:ADNSR"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*01:01", NmdpCode = "*01:CXC"},
                    new AlleleTestData {AlleleName = "*09:01", NmdpCode = "*09:ANZW"},
                    new AlleleTestData {AlleleName = "*104:01", NmdpCode = "*104:ADNSR"},
                }
            ),
            valueDqb1: new LocusInfo<List<AlleleTestData>>(
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*03:09", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:VDUX", Serology = "3"},
                    new AlleleTestData {AlleleName = "*03:191", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:AUXZT", Serology = "3"},
                    new AlleleTestData {AlleleName = "*06:110", PGroup = "06:03P", GGroup = "06:03:01G", NmdpCode = "*06:ACSRM", Serology = "6"},
                    new AlleleTestData {AlleleName = "*06:39", PGroup = "06:04P", GGroup = "06:04:01G", NmdpCode = "*06:ACSRM", Serology = "6"},
                    new AlleleTestData {AlleleName = "*06:41", PGroup = "06:03P", GGroup = "06:03:01G", NmdpCode = "*06:ACSRM", Serology = "6"},
                    new AlleleTestData {AlleleName = "*06:88", PGroup = "06:09P", GGroup = "06:09:01G", NmdpCode = "*06:ACSRM", Serology = "6"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*02:04", PGroup = "02:01P", GGroup = "02:01:01G", NmdpCode = "*02:YNVM", Serology = "2"},
                    new AlleleTestData {AlleleName = "*03:09", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:VDUX", Serology = "3"},
                    new AlleleTestData {AlleleName = "*03:243", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:AUXZT", Serology = "3"},
                    new AlleleTestData {AlleleName = "*06:110", PGroup = "06:03P", GGroup = "06:03:01G", NmdpCode = "*06:ACSRM", Serology = "6"},
                    new AlleleTestData {AlleleName = "*06:41", PGroup = "06:03P", GGroup = "06:03:01G", NmdpCode = "*06:ACSRM", Serology = "6"},
                    new AlleleTestData {AlleleName = "*06:88", PGroup = "06:09P", GGroup = "06:09:01G", NmdpCode = "*06:ACSRM", Serology = "6"},
                }
            ),
            valueDrb1: new LocusInfo<List<AlleleTestData>>(
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*03:124", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:AMMSZ", Serology = "3"},
                    new AlleleTestData {AlleleName = "*11:129", PGroup = "11:06P", GGroup = "11:06:01G", NmdpCode = "*11:ACSSH", Serology = "11"},
                },
                new List<AlleleTestData>
                {
                    new AlleleTestData {AlleleName = "*03:124", PGroup = "03:01P", GGroup = "03:01:01G", NmdpCode = "*03:AMMSZ", Serology = "3"},
                    new AlleleTestData {AlleleName = "*11:129", PGroup = "11:06P", GGroup = "11:06:01G", NmdpCode = "*11:ACSSH", Serology = "11"},
                    new AlleleTestData {AlleleName = "*11:198", PGroup = "11:04P", GGroup = "11:04:01G", NmdpCode = "*11:ASEVD", Serology = "11"},
                }
            )
        );
    }
}