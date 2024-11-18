using System.Collections.Generic;
using Atlas.Client.Models.Common.Requests;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.ManualTesting.Common.Models;
using Atlas.ManualTesting.Common.Services;
using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Services.Scoring;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;

namespace Atlas.ManualTesting.Services.WmdaConsensusResults.Scorers
{
    internal abstract class WmdaExerciseScorerBase : ScoreRequestProcessor
    {
        /// <inheritdoc />
        protected WmdaExerciseScorerBase(IFileReader<ImportedSubject> subjectReader, IScoreBatchRequester scoreBatchRequester) 
            : base(subjectReader, scoreBatchRequester)
        {
        }

        /// <inheritdoc />
        protected override ScoringCriteria BuildScoringCriteria()
        {
            return new ScoringCriteria
            {
                LociToScore = new[] { Locus.A, Locus.B, Locus.Drb1 },
                LociToExcludeFromAggregateScore = new List<Locus>()
            };
        }

        protected static WmdaConsensusResultsFile BuildConsensusResultsFile(string patientId, string donorId, ScoringResult scoringResult)
        {
            static string CountTotalMismatches(LocusSearchResult locusResult) => $"{2 - locusResult.MatchCount}";

            return new WmdaConsensusResultsFile
            {
                PatientId = patientId,
                DonorId = donorId,
                MismatchCountAtA = CountTotalMismatches(scoringResult.SearchResultAtLocusA),
                MismatchCountAtB = CountTotalMismatches(scoringResult.SearchResultAtLocusB),
                MismatchCountAtDrb1 = CountTotalMismatches(scoringResult.SearchResultAtLocusDrb1)
            };
        }
    }
}
