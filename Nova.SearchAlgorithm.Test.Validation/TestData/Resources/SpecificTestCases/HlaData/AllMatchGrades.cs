using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Resources.SpecificTestCases.HlaData
{
    public class AllMatchGrades
    {
        public static PhenotypeInfo<string> PatientHla { get; } = new LocusInfo<string>
        {
            A = "*11:01:01:01",
            B = "*27:04:01",
            C = "*02:02:02:02",
            DPB1 = "*04:01:01:01",
            DQB1 = "*05:03:01:01",
            DRB1 = "*13:01:01:01",
        }.ToPhenotypeInfo((l, hla) => hla);
        
        public static IEnumerable<PhenotypeInfo<string>> DonorHlaSets { get; } = new List<LocusInfo<string>>
        {
            // gDNA match
            new LocusInfo<string>
            {
                A = "*11:01:01:01",
                B = "*27:04:01",
                C = "*02:02:02:02",
                DPB1 = "*04:01:01:01",
                DQB1 = "*05:03:01:01",
                DRB1 = "*13:01:01:01",
            },
            
            // cDNA match
            new LocusInfo<string>
            {
                A = "*11:01",
                B = "*27:04",
                C = "*02:02",
                DPB1 = "*04:01",
                DQB1 = "*05:03",
                DRB1 = "*13:01",
            },
            
            // protein match
            new LocusInfo<string>
            {
                A = "*11:01:75",
                B = "*27:04:05",
                C = "*02:02:01",
                DPB1 = "*04:01:02",
                DQB1 = "*05:03:17",
                DRB1 = "*13:01:22",
            },
            
            // g-group match
            new LocusInfo<string>
            {
                A = "*11:86",
                B = "*27:69",
                C = "*02:69",
                DPB1 = "*415:01",
                DQB1 = "*05:42",
                DRB1 = "*13:215",
            },
            
            // p-group match
            new LocusInfo<string>
            {
                A = "*11:107",
                B = "*27:112",
                C = "*02:10:02",
                DPB1 = "*677:01",
                DQB1 = "*05:43:01",
                DRB1 = "*13:238",
            },
            
            // serology match
            new LocusInfo<string>
            {
                A = "11",
                B = "27",
                C = "2",
                DPB1 = "4",
                DQB1 = "5",
                DRB1 = "13",
            },
        }.Select(locusInfo => locusInfo.ToPhenotypeInfo((l, hla) => hla));
    }
}