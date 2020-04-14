using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Common.Models;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.SpecificTestCases.HlaData
{
    /// <summary>
    /// No DQB data was found that has botha p-group and protein level match possible.
    /// We have not ruled out the possibility of such test data - but an 8/8 search is enough to run the ranking tests, so it is not necessary
    /// </summary>
    public class AllMatchGrades
    {
        public static PhenotypeInfo<string> PatientHla { get; } = new LocusInfo<string>
        {
            A = "*11:01:01:01",
            B = "*35:01:01:01",
            C = "*02:02:02:02",
            DPB1 = "*04:01:01:01",
            DRB1 = "*13:01:01:01",
        }.ToPhenotypeInfo((l, hla) => hla);
        
        public static IEnumerable<PhenotypeInfo<string>> DonorHlaSets { get; } = new List<LocusInfo<string>>
        {
            // gDNA match
            new LocusInfo<string>
            {
                A = "*11:01:01:01",
                B = "*35:01:01:01",
                C = "*02:02:02:02",
                DPB1 = "*04:01:01:01",
                DRB1 = "*13:01:01:01",
            },
            
            // cDNA match
            new LocusInfo<string>
            {
                A = "*11:01",
                B = "*35:01:01:02",
                C = "*02:02",
                DPB1 = "*04:01",
                DRB1 = "*13:01",
            },
            
            // protein match
            new LocusInfo<string>
            {
                A = "*11:01:75",
                B = "*35:01:02",
                C = "*02:02:01",
                DPB1 = "*04:01:02",
                DRB1 = "*13:01:22",
            },
            
            // g-group match
            new LocusInfo<string>
            {
                A = "*11:86",
                B = "*35:336",
                C = "*02:69",
                DPB1 = "*415:01",
                DRB1 = "*13:215",
            },
            
            // p-group match
            new LocusInfo<string>
            {
                A = "*11:107",
                B = "*35:330",
                C = "*02:10:02",
                DPB1 = "*677:01",
                DRB1 = "*13:238",
            },
            
            // serology match
            new LocusInfo<string>
            {
                A = "11",
                B = "35",
                C = "2",
                DPB1 = "4",
                DRB1 = "13",
            },
        }.Select(locusInfo => locusInfo.ToPhenotypeInfo((l, hla) => hla));
    }
}