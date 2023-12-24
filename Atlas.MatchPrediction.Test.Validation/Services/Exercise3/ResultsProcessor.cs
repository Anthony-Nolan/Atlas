using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Validation.Services.Exercise3
{
    public interface IMatchPredictionResultsProcessor
    {
        Task ProcessAndStoreResults(IReadOnlyCollection<MatchPredictionResultLocation> resultLocations);
    }

    internal class MatchPredictionResultsProcessor : IMatchPredictionResultsProcessor
    {
        private readonly IMatchPredictionRequestRepository requestRepository;
        private readonly IMatchPredictionResultsRepository resultsRepository;
        private readonly IBlobStreamer resultsStreamer;

        public MatchPredictionResultsProcessor(
            IMatchPredictionRequestRepository requestRepository,
            IMatchPredictionResultsRepository resultsRepository,
            IBlobStreamer resultsStreamer)
        {
            this.requestRepository = requestRepository;
            this.resultsRepository = resultsRepository;
            this.resultsStreamer = resultsStreamer;
        }

        public async Task ProcessAndStoreResults(IReadOnlyCollection<MatchPredictionResultLocation> resultLocations)
        {
            var algorithmIds = resultLocations.Select(l => l.MatchPredictionRequestId);
            var requests = await requestRepository.GetMatchPredictionRequests(algorithmIds);

            if (requests.IsNullOrEmpty())
            {
                Debug.WriteLine("No match requests found for submitted algorithm IDs.");
                return;
            }

            var identifiedResults = requests.Select(r => new IdentifiedResults
            {
                MatchPredictionRequestId = r.Id,
                AlgorithmRequestId = r.MatchPredictionAlgorithmRequestId,
                ResultLocation = resultLocations.Single(l => l.MatchPredictionRequestId == r.MatchPredictionAlgorithmRequestId)
            }).ToList();

            await FetchAndPersistResults(identifiedResults);
        }

        private async Task FetchAndPersistResults(IEnumerable<IdentifiedResults> identifiedResults)
        {
            var downloadTasks = identifiedResults.Select(DownloadResults);
            var downloadedResults = await Task.WhenAll(downloadTasks);

            var extractedResults = downloadedResults.SelectMany(ExtractResults).ToList();
            await PersistResults(extractedResults);
        }

        private async Task<IdentifiedResults> DownloadResults(IdentifiedResults result)
        {
            var blobStream = await resultsStreamer.GetBlobContents(
                result.ResultLocation.BlobStorageContainerName, result.ResultLocation.FileName);
            var contents = await new StreamReader(blobStream).ReadToEndAsync();
            result.MatchProbabilityResponse = JsonConvert.DeserializeObject<MatchProbabilityResponse>(contents);

            return result;
        }

        private static IReadOnlyCollection<MatchPredictionResults> ExtractResults(IdentifiedResults result)
        {
            var perDonorResults = BuildResults(
                result.MatchPredictionRequestId, null, result.MatchProbabilityResponse.MatchProbabilities);
            var perLocusResults = BuildLocusMatchProbabilities(
                result.MatchPredictionRequestId, result.MatchProbabilityResponse.MatchProbabilitiesPerLocusTransfer);

            return perDonorResults.Concat(perLocusResults).ToList();
        }

        private static IEnumerable<MatchPredictionResults> BuildResults(
            int requestId,
            Locus? locus,
            MatchProbabilities probabilities)
        {
            return new List<MatchPredictionResults>
            {
                new()
                {
                    MatchPredictionRequestId = requestId,
                    Locus = locus,
                    MismatchCount = 0,
                    Probability = probabilities.ZeroMismatchProbability?.Decimal
                },
                new()
                {
                    MatchPredictionRequestId = requestId,
                    Locus = locus,
                    MismatchCount = 1,
                    Probability = probabilities.OneMismatchProbability?.Decimal
                },
                new()
                {
                    MatchPredictionRequestId = requestId,
                    Locus = locus,
                    MismatchCount = 2,
                    Probability = probabilities.TwoMismatchProbability?.Decimal
                },
            };
        }
        private static IEnumerable<MatchPredictionResults> BuildLocusMatchProbabilities(
            int matchedDonorId,
            LociInfoTransfer<MatchProbabilityPerLocusResponse> matchProbabilitiesPerLocus)
        {
            var lociInfo = matchProbabilitiesPerLocus.ToLociInfo();
            return MatchPredictionStaticData.MatchPredictionLoci
                .SelectMany(l => BuildResults(matchedDonorId, l, lociInfo.GetLocus(l).MatchProbabilities));
        }

        private async Task PersistResults(IReadOnlyCollection<MatchPredictionResults> results)
        {
            var requestIds = results.Select(r => r.MatchPredictionRequestId).Distinct();
            await resultsRepository.DeleteExistingResults(requestIds);
            await resultsRepository.BulkInsert(results);
        }

        private class IdentifiedResults
        {
            public int MatchPredictionRequestId { get; set; }
            public string AlgorithmRequestId { get; set; }
            public MatchPredictionResultLocation ResultLocation { get; set; }
            public MatchProbabilityResponse MatchProbabilityResponse { get; set; }
        }
    }
}