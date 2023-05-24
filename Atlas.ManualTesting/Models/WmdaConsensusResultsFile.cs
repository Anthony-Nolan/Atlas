using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.ManualTesting.Models
{
    public class WmdaConsensusResultsFile
    {
        public string PatientId { get; set; }
        public string DonorId { get; set; }
        public string MismatchCountAtA { get; set; }
        public string MismatchCountAtB { get; set; }
        public string MismatchCountAtDrb1 { get; set; }

        public IDictionary<Locus, string> TotalMismatchCounts => new Dictionary<Locus, string>
        {
            { Locus.A, MismatchCountAtA },
            { Locus.B , MismatchCountAtB },
            { Locus.Drb1 , MismatchCountAtDrb1 }
        };

        public override string ToString()
        {
            return $"{PatientId};{DonorId};{MismatchCountAtA};{MismatchCountAtB};{MismatchCountAtDrb1}";
        }
    }

    public class WmdaConsensusResultsFileSetTwo : WmdaConsensusResultsFile
    {
        public string AntigenMismatchCountAtA { get; set; }
        public string AntigenMismatchCountAtB { get; set; }
        public string AntigenMismatchCountAtDrb1 { get; set; }

        public IDictionary<Locus, string> AntigenMismatchCounts => new Dictionary<Locus, string>
        {
            { Locus.A, AntigenMismatchCountAtA },
            { Locus.B , AntigenMismatchCountAtB },
            { Locus.Drb1 , AntigenMismatchCountAtDrb1 }
        };

        public override string ToString()
        {
            return $"{PatientId};{DonorId};" +
                   $"{MismatchCountAtA};{AntigenMismatchCountAtA};" +
                   $"{MismatchCountAtB};{AntigenMismatchCountAtB};" +
                   $"{MismatchCountAtDrb1};{AntigenMismatchCountAtDrb1}";
        }
    }
}