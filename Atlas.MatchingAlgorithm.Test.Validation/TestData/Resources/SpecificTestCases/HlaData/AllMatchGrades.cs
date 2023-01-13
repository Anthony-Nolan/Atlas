using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.SpecificTestCases.HlaData
{
    /// <summary>
    /// No DQB data was found that has both a p-group and protein level match possible.
    /// We have not ruled out the possibility of such test data - but an 8/8 search is enough to run the ranking tests, so it is not necessary
    /// </summary>
    public class AllMatchGrades
    {
        public static PhenotypeInfo<string> PatientHla { get; } = new LociInfo<string>
        (
            valueA: "*11:01:01:01",
            valueB: "*35:01:01:01",
            valueC: "*02:02:02:02",
            valueDpb1: "*04:01:01:01",
            valueDrb1: "*13:01:01:01"
        ).ToPhenotypeInfo((l, hla) => hla);
        
        public static IEnumerable<PhenotypeInfo<string>> DonorHlaSets { get; } = new List<LociInfo<string>>
        {
            // gDNA match
            new LociInfo<string>
            (
                valueA: "*11:01:01:01",
                valueB: "*35:01:01:01",
                valueC: "*02:02:02:02",
                valueDpb1: "*04:01:01:01",
                valueDrb1: "*13:01:01:01"
            ),
            
            // cDNA match
            new LociInfo<string>
            (
                valueA: "*11:01",
                valueB: "*35:01:01:02",
                valueC: "*02:02",
                valueDpb1: "*04:01",
                valueDrb1: "*13:01"
            ),
            
            // protein match
            new LociInfo<string>
            (
                valueA: "*11:01:75",
                valueB: "*35:01:02",
                valueC: "*02:02:01",
                valueDpb1: "*04:01:02",
                valueDrb1: "*13:01:22"
            ),
            
            // g-group match
            new LociInfo<string>
            (
                valueA: "*11:86",
                valueB: "*35:336",
                valueC: "*02:69",
                valueDpb1: "*415:01",
                valueDrb1: "*13:215"
            ),
            
            // p-group match
            new LociInfo<string>
            (
                valueA: "*11:107",
                valueB: "*35:330",
                valueC: "*02:10:02",
                valueDpb1: "*677:01",
                valueDrb1: "*13:238"
            ),
            
            // serology match
            new LociInfo<string>
            (
                valueA: "11",
                valueB: "35",
                valueC: "2",
                valueDpb1: "4",
                valueDrb1: "13"
            ),
        }.Select(lociInfo => lociInfo.ToPhenotypeInfo((l, hla) => hla));
    }
}