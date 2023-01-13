using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.Alleles
{
    /// <summary>
    /// This test data was manually curated from alleles with an expression suffix other than 'N' (aka null) from the SOLAR database
    /// Possible values = C, S, Q, L, A
    /// Some of these have no corresponding alleles in SOLAR, and others only at certain loci
    ///
    /// As no functionality of the algorithm is affected, we are treating all suffixes interchangeably
    /// 
    /// No NMDP codes are selected for the selected alleles, it is assumed this data will only be used for allele level resolution.
    /// If NMDP resolution tests are necessary, a new dataset will need to be curated
    /// </summary>
    public static class AllelesWithNonNullExpressionSuffix
    {
        public static readonly LociInfo<List<AlleleTestData>> Alleles = new LociInfo<List<AlleleTestData>>
        (
            valueA: new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*32:11Q"},
                new AlleleTestData {AlleleName = "*11:170Q"},
                new AlleleTestData {AlleleName = "*24:02:01:02L"},
                new AlleleTestData {AlleleName = "*30:14L"},
            },
            valueB: new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*39:01L"},
                new AlleleTestData {AlleleName = "*44:02:01:02S"},
                new AlleleTestData {AlleleName = "*56:01:01:05S"},
                new AlleleTestData {AlleleName = "*40:133Q"},
                new AlleleTestData {AlleleName = "*27:05:02:04Q"},
            },
            valueC: new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*03:22Q"},
                new AlleleTestData {AlleleName = "*07:150Q"},
                new AlleleTestData {AlleleName = "*03:244Q"},
                new AlleleTestData {AlleleName = "*15:105Q"},
            },
            valueDpb1: new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*697:01Q"},
            },
            valueDqb1: new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "*03:91Q"},
                new AlleleTestData {AlleleName = "*03:99Q"},
                new AlleleTestData {AlleleName = "*05:132Q"},
                new AlleleTestData {AlleleName = "*02:53Q"},
            },
            // No alleles with any expression suffix exist in DR_ANTIGENS for Locus DRB1
            valueDrb1: new List<AlleleTestData>
            {
            }
        );
    }
}