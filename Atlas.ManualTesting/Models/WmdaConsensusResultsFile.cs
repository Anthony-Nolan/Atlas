using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.ManualTesting.Models
{
    public class WmdaConsensusResultsFile
    {
        public string PatientId { get; set; }
        public string DonorId { get; set; }
        public int? MismatchCountAtA { get; set; }
        public int? MismatchCountAtB { get; set; }
        public int? MismatchCountAtDrb1 { get; set; }

        public WmdaConsensusResultsFile(string patientId, string donorId, ScoringResult result)
        {
            static int? CountMismatches(LocusSearchResult locusResult) => 2 - locusResult.MatchCount;

            PatientId = patientId;
            DonorId = donorId;
            MismatchCountAtA = CountMismatches(result.SearchResultAtLocusA);
            MismatchCountAtB = CountMismatches(result.SearchResultAtLocusB);
            MismatchCountAtDrb1 = CountMismatches(result.SearchResultAtLocusDrb1);
        }

        public override string ToString()
        {
            return $"{PatientId};{DonorId};{MismatchCountAtA};{MismatchCountAtB};{MismatchCountAtDrb1}";
        }
    }

    public class WmdaConsensusResultsFileSetTwo : WmdaConsensusResultsFile
    {
        public int AntigenMismatchCountAtA { get; set; }
        public int AntigenMismatchCountAtB { get; set; }
        public int AntigenMismatchCountAtDrb1 { get; set; }

        public WmdaConsensusResultsFileSetTwo(string patientId, string donorId, ScoringResult result) : base(patientId, donorId, result)
        {
            static int CountAntigenMismatches(LocusSearchResult locusResult)
            {
                return new List<bool?>
                {
                    locusResult.ScoreDetailsAtPositionOne.IsAntigenMatch,
                    locusResult.ScoreDetailsAtPositionTwo.IsAntigenMatch
                }.Count(x => x.HasValue && !x.Value);
            }

            AntigenMismatchCountAtA = CountAntigenMismatches(result.SearchResultAtLocusA);
            AntigenMismatchCountAtB = CountAntigenMismatches(result.SearchResultAtLocusB);
            AntigenMismatchCountAtDrb1 = CountAntigenMismatches(result.SearchResultAtLocusDrb1);
        }

        public override string ToString()
        {
            return $"{PatientId};{DonorId};" + 
                   $"{MismatchCountAtA};{AntigenMismatchCountAtA};" + 
                   $"{MismatchCountAtB};{AntigenMismatchCountAtB};" + 
                   $"{MismatchCountAtDrb1};{AntigenMismatchCountAtDrb1}";
        }
    }
}