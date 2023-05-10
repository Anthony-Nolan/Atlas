using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Services.Scoring;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.ManualTesting.Functions
{
    /// <summary>
    /// Functions that process the WMDA consensus datasets for exercises that involve the counting of mismatches.
    /// </summary>
    public class WmdaConsensusDatasetScoringFunctions
    {
        private readonly IScoreRequestProcessor scoreRequestProcessor;

        public WmdaConsensusDatasetScoringFunctions(IScoreRequestProcessor scoreRequestProcessor)
        {
            this.scoreRequestProcessor = scoreRequestProcessor;
        }

        [FunctionName(nameof(ProcessWmdaConsensusDataset_Exercise1))]
        public async Task ProcessWmdaConsensusDataset_Exercise1(
            [RequestBodyType(typeof(ImportAndScoreRequest), nameof(ImportAndScoreRequest))]
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            var importAndScoreRequest = JsonConvert.DeserializeObject<ImportAndScoreRequest>(await new StreamReader(request.Body).ReadToEndAsync());

            await scoreRequestProcessor.ProcessScoreRequest(new ScoreRequestProcessorInput
            {
                ImportAndScoreRequest = importAndScoreRequest,
                ScoringCriteria = BuildThreeLocusScoringCriteria(),
                ResultTransformer = TransformScoringResultForExercise1
            });
        }

        [FunctionName(nameof(ProcessWmdaConsensusDataset_Exercise2))]
        public async Task ProcessWmdaConsensusDataset_Exercise2(
            [RequestBodyType(typeof(ImportAndScoreRequest), nameof(ImportAndScoreRequest))]
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            var importAndScoreRequest = JsonConvert.DeserializeObject<ImportAndScoreRequest>(await new StreamReader(request.Body).ReadToEndAsync());

            await scoreRequestProcessor.ProcessScoreRequest(new ScoreRequestProcessorInput
            {
                ImportAndScoreRequest = importAndScoreRequest,
                ScoringCriteria = BuildThreeLocusScoringCriteria(),
                ResultTransformer = TransformScoringResultForExercise2
            });
        }

        private static ScoringCriteria BuildThreeLocusScoringCriteria()
        {
            return new ScoringCriteria
            {
                LociToScore = new[] { Locus.A, Locus.B, Locus.Drb1 },
                LociToExcludeFromAggregateScore = new List<Locus>()
            };
        }

        private static string TransformScoringResultForExercise1(string patientId, string donorId, ScoringResult result)
        {
            static string CountMismatches(LocusSearchResult locusResult) => $"{2-locusResult.MatchCount}";

            return $"{patientId};{donorId};" +
                   $"{CountMismatches(result.SearchResultAtLocusA)};" +
                   $"{CountMismatches(result.SearchResultAtLocusB)};" +
                   $"{CountMismatches(result.SearchResultAtLocusDrb1)}";
        }

        private static string TransformScoringResultForExercise2(string patientId, string donorId, ScoringResult result)
        {
            static string CountMismatches(LocusSearchResult locusResult) => $"{2 - locusResult.MatchCount}";
            static int CountAntigenMismatches(LocusSearchResult locusResult)
            {
                return new List<bool?>
                {
                    locusResult.ScoreDetailsAtPositionOne.IsAntigenMatch,
                    locusResult.ScoreDetailsAtPositionTwo.IsAntigenMatch
                }.Count(x => x.HasValue && !x.Value);
            }

            return $"{patientId};{donorId};" +
                   $"{CountMismatches(result.SearchResultAtLocusA)};{CountAntigenMismatches(result.SearchResultAtLocusA)};" +
                   $"{CountMismatches(result.SearchResultAtLocusB)};{CountAntigenMismatches(result.SearchResultAtLocusB)};" +
                   $"{CountMismatches(result.SearchResultAtLocusDrb1)};{CountAntigenMismatches(result.SearchResultAtLocusDrb1)}";
        }
    }
}