using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.ManualTesting.Common.Models;
using Atlas.ManualTesting.Common.Services;
using Atlas.ManualTesting.Services.Scoring;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;

namespace Atlas.ManualTesting.Services.WmdaConsensusResults.Scorers
{
    public interface IWmdaExerciseOneScorer : IScoreRequestProcessor
    {
    }

    internal class WmdaExerciseOneScorer : WmdaExerciseScorerBase, IWmdaExerciseOneScorer
    {
        /// <inheritdoc />
        public WmdaExerciseOneScorer(IFileReader<ImportedSubject> subjectReader, IScoreBatchRequester scoreBatchRequester) : base(subjectReader, scoreBatchRequester)
        {
        }

        /// <inheritdoc />
        protected override string TransformResultForReporting(string patientId, string donorId, ScoringResult scoringResult)
        {
            return BuildConsensusResultsFile(patientId, donorId, scoringResult).ToString();
        }

        /// <inheritdoc />
        protected override string TransformResultForLogging(string patientId, string donorId, ScoringResult scoringResult)
        {
            static string MatchGrades(LocusSearchResult locusResult) =>
                $"{locusResult.ScoreDetailsAtPositionOne.MatchGrade};{locusResult.ScoreDetailsAtPositionTwo.MatchGrade}";

            return $"{patientId};{donorId};" +
                   $"{MatchGrades(scoringResult.SearchResultAtLocusA)};" +
                   $"{MatchGrades(scoringResult.SearchResultAtLocusB)};" +
                   $"{MatchGrades(scoringResult.SearchResultAtLocusDrb1)};";
        }
    }
}
