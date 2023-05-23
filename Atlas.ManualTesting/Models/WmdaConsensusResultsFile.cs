using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Empty constructor needed for reading results from files
        /// </summary>
        public WmdaConsensusResultsFile()
        {
        }

        public WmdaConsensusResultsFile(string patientId, string donorId, ScoringResult result)
        {
            static string CountMismatches(LocusSearchResult locusResult) => $"{2 - locusResult.MatchCount}";

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
        public string AntigenMismatchCountAtA { get; set; }
        public string AntigenMismatchCountAtB { get; set; }
        public string AntigenMismatchCountAtDrb1 { get; set; }

        public IDictionary<Locus, string> AntigenMismatchCounts => new Dictionary<Locus, string>
        {
            { Locus.A, AntigenMismatchCountAtA },
            { Locus.B , AntigenMismatchCountAtB },
            { Locus.Drb1 , AntigenMismatchCountAtDrb1 }
        };

        /// <summary>
        /// Empty constructor needed for reading results from files
        /// </summary>
        public WmdaConsensusResultsFileSetTwo()
        {
        }

        public WmdaConsensusResultsFileSetTwo(string patientId, string donorId, ScoringResult result) : base(patientId, donorId, result)
        {
            static string CountAntigenMismatches(LocusSearchResult locusResult)
            {
                return new List<bool?>
                {
                    locusResult.ScoreDetailsAtPositionOne.IsAntigenMatch,
                    locusResult.ScoreDetailsAtPositionTwo.IsAntigenMatch
                }.Count(x => x.HasValue && !x.Value).ToString();
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