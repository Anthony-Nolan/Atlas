using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.SpecificTestCases.HlaData
{
    /// <summary>
    /// No DQB data was found that has both a p-group and protein level match possible.
    /// We have not ruled out the possibility of such test data - but an 8/8 search is enough to run the ranking tests, so it is not necessary
    /// </summary>
    public class AllMatchGrades
    {
        public static PhenotypeInfo<string> PatientHla { get; } = new LociInfo<string>
        {
            A = "*11:01:01:01",
            B = "*35:01:01:01",
            C = "*02:02:02:02",
            Dpb1 = "*04:01:01:01",
            Drb1 = "*13:01:01:01",
        }.ToPhenotypeInfo((l, hla) => hla);
        
        public static IEnumerable<PhenotypeInfo<string>> DonorHlaSets { get; } = new List<LociInfo<string>>
        {
            // gDNA match
            new LociInfo<string>
            {
                A = "*11:01:01:01",
                B = "*35:01:01:01",
                C = "*02:02:02:02",
                Dpb1 = "*04:01:01:01",
                Drb1 = "*13:01:01:01",
            },
            
            // cDNA match
            new LociInfo<string>
            {
                A = "*11:01",
                B = "*35:01:01:02",
                C = "*02:02",
                Dpb1 = "*04:01",
                Drb1 = "*13:01",
            },
            
            // protein match
            new LociInfo<string>
            {
                A = "*11:01:75",
                B = "*35:01:02",
                C = "*02:02:01",
                Dpb1 = "*04:01:02",
                Drb1 = "*13:01:22",
            },
            
            // g-group match
            new LociInfo<string>
            {
                A = "*11:86",
                B = "*35:336",
                C = "*02:69",
                Dpb1 = "*415:01",
                Drb1 = "*13:215",
            },
            
            // p-group match
            new LociInfo<string>
            {
                A = "*11:107",
                B = "*35:330",
                C = "*02:10:02",
                Dpb1 = "*677:01",
                Drb1 = "*13:238",
            },
            
            // serology match
            new LociInfo<string>
            {
                A = "11",
                B = "35",
                C = "2",
                Dpb1 = "4",
                Drb1 = "13",
            },
        }.Select(lociInfo => lociInfo.ToPhenotypeInfo((l, hla) => hla));
    }
}