using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.ManualTesting.Common.Models;
using Atlas.ManualTesting.Common.Services;
using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Services.Scoring;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;

namespace Atlas.ManualTesting.Services.WmdaConsensusResults.Scorers
{
    public interface IWmdaExerciseTwoScorer : IScoreRequestProcessor
    {
    }

    internal class WmdaExerciseTwoScorer : WmdaExerciseScorerBase, IWmdaExerciseTwoScorer
    {
        /// <inheritdoc />
        public WmdaExerciseTwoScorer(IFileReader<ImportedSubject> subjectReader, IScoreBatchRequester scoreBatchRequester)
            : base(subjectReader, scoreBatchRequester)
        {
        }

        /// <inheritdoc />
        protected override string TransformResultForReporting(string patientId, string donorId, ScoringResult scoringResult)
        {
            var resultsFile = BuildConsensusResultsFile(patientId, donorId, scoringResult);

            return new WmdaConsensusResultsFileSetTwo
            {
                PatientId = patientId,
                DonorId = donorId,
                MismatchCountAtA = resultsFile.MismatchCountAtA,
                MismatchCountAtB = resultsFile.MismatchCountAtB,
                MismatchCountAtDrb1 = resultsFile.MismatchCountAtDrb1,
                AntigenMismatchCountAtA = CountAntigenMismatches(scoringResult.SearchResultAtLocusA),
                AntigenMismatchCountAtB = CountAntigenMismatches(scoringResult.SearchResultAtLocusB),
                AntigenMismatchCountAtDrb1 = CountAntigenMismatches(scoringResult.SearchResultAtLocusDrb1)
            }.ToString();
        }

        /// <inheritdoc />
        protected override string TransformResultForLogging(string patientId, string donorId, ScoringResult scoringResult)
        {
            static string AntigenMatch(bool? match) => 
                match switch { true => "Y", false => "N", null => "U" };

            static string PositionInfo(LocusPositionScoreDetails position) => 
                $"{position.MatchGrade},AgM_{AntigenMatch(position.IsAntigenMatch)}";

            static string LocusInfo(LocusSearchResult locusResult) =>
                $"{PositionInfo(locusResult.ScoreDetailsAtPositionOne)};{PositionInfo(locusResult.ScoreDetailsAtPositionTwo)}";

            return $"{patientId};{donorId};" +
                   $"{LocusInfo(scoringResult.SearchResultAtLocusA)};" +
                   $"{LocusInfo(scoringResult.SearchResultAtLocusB)};" +
                   $"{LocusInfo(scoringResult.SearchResultAtLocusDrb1)};";
        }

        private static string CountAntigenMismatches(LocusSearchResult locusResult)
        {
            return new List<bool?>
            {
                locusResult.ScoreDetailsAtPositionOne.IsAntigenMatch,
                locusResult.ScoreDetailsAtPositionTwo.IsAntigenMatch
            }.Count(x => x == null || !x.Value).ToString();
        }
    }
}