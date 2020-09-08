using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Newtonsoft.Json;
using DonorIds = System.Collections.Generic.IDictionary<int,int>;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification
{
    public interface ISearchResultsFetcher
    {
        Task FetchSearchResults(SearchResultsNotification notification);
    }

    internal class SearchResultsFetcher : ISearchResultsFetcher
    {
        private readonly ISearchRequestsRepository searchRequestsRepository;
        private readonly ISearchResultsStreamer resultsStreamer;
        private readonly IMatchedDonorsRepository matchedDonorsRepository;
        private readonly IMatchProbabilitiesRepository matchProbabilitiesRepository;

        public SearchResultsFetcher(
            ISearchRequestsRepository searchRequestsRepository,
            ISearchResultsStreamer resultsStreamer,
            IMatchedDonorsRepository matchedDonorsRepository,
            IMatchProbabilitiesRepository matchProbabilitiesRepository)
        {
            this.searchRequestsRepository = searchRequestsRepository;
            this.resultsStreamer = resultsStreamer;
            this.matchedDonorsRepository = matchedDonorsRepository;
            this.matchProbabilitiesRepository = matchProbabilitiesRepository;
        }

        public async Task FetchSearchResults(SearchResultsNotification notification)
        {
            var recordId = await searchRequestsRepository.GetRecordIdByAtlasSearchId(notification.SearchRequestId);

            if (recordId == 0)
            {
                Debug.WriteLine($"No record found with Atlas search id {notification.SearchRequestId}.");
                return;
            }

            if (!notification.WasSuccessful)
            {
                await searchRequestsRepository.MarkSearchResultsAsRetrieved(recordId, null, false);
                Debug.WriteLine($"Search request {recordId} was not successful - record updated.");
                return;
            }

            await FetchAndPersistResults(recordId, notification);
        }

        private async Task FetchAndPersistResults(int recordId, SearchResultsNotification notification)
        {
            var resultSet = JsonConvert.DeserializeObject<SearchResultSet>(await DownloadResults(notification));

            var donorIds = await StoreMatchedDonors(recordId, resultSet);
            await StoreMatchProbabilities(donorIds, resultSet);
            
            await searchRequestsRepository.MarkSearchResultsAsRetrieved(recordId, resultSet.TotalResults, true);
            Debug.WriteLine($"Search request {recordId} was successful - {resultSet.TotalResults} matched donors found.");
        }

        private async Task<string> DownloadResults(SearchResultsNotification notification)
        {
            var blobStream = await resultsStreamer.GetSearchResultsBlobContents(
                notification.BlobStorageContainerName, notification.ResultsFileName);
            return await new StreamReader(blobStream).ReadToEndAsync();
        }

        /// <returns>Dictionary with key of <see cref="MatchedDonor.MatchedDonorSimulant_Id"/>and value of
        /// <see cref="MatchedDonor.Id"/></returns>
        private async Task<DonorIds> StoreMatchedDonors(int recordId, SearchResultSet resultSet)
        {
            if (resultSet.TotalResults == 0)
            {
                return new Dictionary<int, int>();
            }

            var matchedDonors = resultSet.SearchResults
                .Select(d => MapToMatchedDonor(recordId, d))
                .ToList();

            // important to delete before insertion to wipe any data from previous storage attempts
            await matchedDonorsRepository.DeleteMatchedDonors(recordId);
            await matchedDonorsRepository.BulkInsertMatchedDonors(matchedDonors);

            return await matchedDonorsRepository.GetMatchedDonorIdsBySimulantIds(
                recordId, matchedDonors.Select(d => d.MatchedDonorSimulant_Id));
        }

        private static MatchedDonor MapToMatchedDonor(int recordId, SearchResult result)
        {
            return new MatchedDonor
            {
                SearchRequestRecord_Id = recordId,
                MatchedDonorSimulant_Id = int.Parse(result.DonorCode),
                TotalMatchCount = result.MatchingResult.MatchingResult.TotalMatchCount,
                TypedLociCount = result.MatchingResult.MatchingResult.TypedLociCount ?? 0,
                WasPatientRepresented = !result.MatchPredictionResult.IsPatientPhenotypeUnrepresented,
                WasDonorRepresented = !result.MatchPredictionResult.IsDonorPhenotypeUnrepresented,
                SearchResult = JsonConvert.SerializeObject(result)
            };
        }

        private async Task StoreMatchProbabilities(DonorIds donorIds, SearchResultSet resultSet)
        {
            if (donorIds.IsNullOrEmpty())
            {
                return;
            }

            var matchProbabilities = donorIds
                .Join(
                    resultSet.SearchResults,
                    ids => ids.Key.ToString(),
                    result => result.DonorCode,
                    (ids, result) => ExtractMatchProbabilities(ids.Value, result))
                .SelectMany(collection => collection)
                .ToList();

            // important to delete before insertion to wipe any data from previous storage attempts
            await matchProbabilitiesRepository.DeleteMatchProbabilities(donorIds.Values);
            await matchProbabilitiesRepository.BulkInsertMatchProbabilities(matchProbabilities);
        }

        private static IReadOnlyCollection<MatchProbability> ExtractMatchProbabilities(int matchedDonorId, SearchResult result)
        {
            var probabilities = BuildMatchProbabilities(
                matchedDonorId, null, result.MatchPredictionResult.MatchProbabilities);
            var locusProbabilities = BuildLocusMatchProbabilities(
                matchedDonorId, result.MatchPredictionResult.MatchProbabilitiesPerLocusTransfer);

            return probabilities.Concat(locusProbabilities).ToList();
        }

        private static IReadOnlyCollection<MatchProbability> BuildMatchProbabilities(
            int matchedDonorId,
            Locus? locus,
            MatchProbabilities probabilities)
        {
            return new List<MatchProbability>
            {
                new MatchProbability
                {
                    MatchedDonor_Id = matchedDonorId,
                    Locus = locus,
                    MismatchCount = 0,
                    Probability = probabilities.ZeroMismatchProbability?.Decimal
                },
                new MatchProbability
                {
                    MatchedDonor_Id = matchedDonorId,
                    Locus = locus,
                    MismatchCount = 1,
                    Probability = probabilities.OneMismatchProbability?.Decimal
                },
                new MatchProbability
                {
                    MatchedDonor_Id = matchedDonorId,
                    Locus = locus,
                    MismatchCount = 2,
                    Probability = probabilities.TwoMismatchProbability?.Decimal
                },
            };
        }
        private static IReadOnlyCollection<MatchProbability> BuildLocusMatchProbabilities(
            int matchedDonorId,
            LociInfoTransfer<MatchProbabilityPerLocusResponse> matchProbabilitiesPerLocus)
        {
            var lociInfo = matchProbabilitiesPerLocus.ToLociInfo();
            return MatchPredictionStaticData.MatchPredictionLoci
                .SelectMany(l => BuildMatchProbabilities(matchedDonorId, l, lociInfo.GetLocus(l).MatchProbabilities))
                .ToList();
        }
    }
}
