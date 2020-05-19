using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.MatchingAlgorithm.Test.Performance.Models
{
    public class TestOutput
    {
        public string PatientId { get; set; }

        public long ElapsedMilliseconds { get; set; }
        public int MatchedDonors { get; set; }
        
        public int? SolarSearchElapsedMilliseconds { get; set; }
        public int? SolarSearchMatchedDonors { get; set; }
        
        public Environment Environment { get; set; }
        public string DatabaseSize { get; set; }

        public SearchType SearchType { get; set; }
        public DonorType DonorType { get; set; }

        public string HlaA1 { get; set; }
        public string HlaA2 { get; set; }
        public string HlaB1 { get; set; }
        public string HlaB2 { get; set; }
        public string HlaC1 { get; set; }
        public string HlaC2 { get; set; }
        public string HlaDqb11 { get; set; }
        public string HlaDqb12 { get; set; }
        public string HlaDrb11 { get; set; }
        public string HlaDrb12 { get; set; }
        
        public string Notes { get; set; }

        public TestOutput(TestInput input, SearchMetrics metrics, string notes = "")
        {
            PatientId = input.PatientId;
            SearchType = input.SearchType;
            DonorType = input.DonorType;

            HlaA1 = input.Hla.A.Position1;
            HlaA2 = input.Hla.A.Position2;
            HlaB1 = input.Hla.B.Position1;
            HlaB2 = input.Hla.B.Position2;
            HlaC1 = input.Hla.C.Position1;
            HlaC2 = input.Hla.C.Position2;
            HlaDqb11 = input.Hla.Dqb1.Position1;
            HlaDqb12 = input.Hla.Dqb1.Position2;
            HlaDrb11 = input.Hla.Drb1.Position1;
            HlaDrb12 = input.Hla.Drb1.Position2;

            ElapsedMilliseconds = metrics.ElapsedMilliseconds;
            MatchedDonors = metrics.DonorsReturned;

            Environment = input.AlgorithmInstanceInfo.Environment;
            DatabaseSize = input.AlgorithmInstanceInfo.DatabaseSize;

            SolarSearchElapsedMilliseconds = input.SolarSearchElapsedMilliseconds;
            SolarSearchMatchedDonors = input.SolarSearchMatchedDonors;

            Notes = notes;
        }
    }
}