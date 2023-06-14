using Atlas.Client.Models.Search.Results.LogFile;
using Atlas.Common.AzureStorage.Blob;
using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.ManualTesting.Services
{
    public interface IWmdaParallelRunResultsHandler
    {
        Task<(string PerformanceInfoFileName, string FailedSearchesFileName)> Handle(WmdaParallelSearchInfoPeekRequest request);
    }

    internal class WmdaParallelRunResultsHandler : IWmdaParallelRunResultsHandler
    {
        private readonly AzureStorageSettings azureStorageSettings;
        private readonly ISearchResultNotificationsPeeker notificationsPeeker;
        private readonly IBlobDownloader blobDownloader;

        public WmdaParallelRunResultsHandler(ISearchResultNotificationsPeeker notificationsPeeker, IOptions<AzureStorageSettings> azureStorageSettings, IBlobDownloader blobDownloader)
        {
            this.azureStorageSettings = azureStorageSettings.Value;
            this.notificationsPeeker = notificationsPeeker;
            this.blobDownloader = blobDownloader;
        }

        public async Task<(string PerformanceInfoFileName, string FailedSearchesFileName)> Handle(WmdaParallelSearchInfoPeekRequest request)
        {
            var notifications = await notificationsPeeker.GetSearchResultsNotifications(request);

            var performanceInfoResults = new List<WmdaParallelRunPerformanceInfo>();
            var failedSearchResults = new List<WmdaParallelRunFailedSearch>();

            foreach (var notification in notifications.PeekedNotifications)
            {
                var resultItem = new WmdaParallelRunPerformanceInfo
                {
                    SearchRequestId = notification.SearchRequestId,
                    DonorCount = notification.NumberOfResults,
                    WasSuccessful = notification.WasSuccessful
                };

                if (!notification.WasSuccessful)
                {
                    failedSearchResults.Add(new () 
                    {
                        SearchRequestId = notification.SearchRequestId,
                        FailureInfo = JsonConvert.SerializeObject(notification.FailureInfo)
                    });
                }

                var logFilename = $"{notification.SearchRequestId}-log.json";

                try
                {
                    var matchingAlgorithmResultsLog = await blobDownloader.Download<SearchLog>(azureStorageSettings.MatchingAlgorithmResultsBlobContainer, logFilename);

                    resultItem.MatchingQueueDuration = matchingAlgorithmResultsLog.RequestPerformanceMetrics.StartTime - matchingAlgorithmResultsLog.RequestPerformanceMetrics.InitiationTime;
                    resultItem.MatchingRequestDuration = matchingAlgorithmResultsLog.RequestPerformanceMetrics.Duration;
                    resultItem.MatchingInitiationTime = matchingAlgorithmResultsLog.RequestPerformanceMetrics.InitiationTime;
                }
                catch { }

                try
                {
                    var searchResultsLog = await blobDownloader.Download<SearchLog>(azureStorageSettings.SearchResultsBlobContainer, logFilename);

                    resultItem.MatchPredictionQueueDuration = searchResultsLog.RequestPerformanceMetrics.StartTime - searchResultsLog.RequestPerformanceMetrics.InitiationTime;
                    resultItem.MatchPredictionRequestDuration = searchResultsLog.RequestPerformanceMetrics.Duration;
                    resultItem.MatchPredictionCompletionTime = searchResultsLog.RequestPerformanceMetrics.CompletionTime;
                }
                catch { }

                performanceInfoResults.Add(resultItem);
            }

            return await SaveResults( request, performanceInfoResults, failedSearchResults );
        }

        private async Task<(string PerformanceInfoFileName, string FailedSearchesFileName)> SaveResults(WmdaParallelSearchInfoPeekRequest request,
            List<WmdaParallelRunPerformanceInfo> performanceInfoResults, List<WmdaParallelRunFailedSearch> failedSearchResults)
        {
            (string performanceInfoFileName, string failedSearchesFileName) = (null, null);

            var targetDirectory = request.Directory ?? Directory.GetCurrentDirectory();

            if (performanceInfoResults.Any())
            {
                if (!Directory.Exists(targetDirectory))
                    Directory.CreateDirectory(targetDirectory);

                performanceInfoFileName = Path.Combine(targetDirectory, $"search-info_{request.FromSequenceNumber}-{request.MessageCount}_performance-info.csv");
                var performanceInfoCsv = new StringBuilder(
                    "request ID,was successful,matching queue duration,matching request duration,match prediction queue duration,match predition request duration,matching initiation time,match prediction completion time,donor count\n");
                performanceInfoResults.ForEach(r => performanceInfoCsv.AppendLine(
                    $"{r.SearchRequestId},{r.WasSuccessful},{r.MatchingQueueDuration},{r.MatchingRequestDuration},{r.MatchPredictionQueueDuration},{r.MatchPredictionRequestDuration},{r.MatchingInitiationTime},{r.MatchPredictionCompletionTime},{r.DonorCount}"));
                await File.WriteAllTextAsync(performanceInfoFileName, performanceInfoCsv.ToString());
            }

            if (failedSearchResults.Any())
            {
                failedSearchesFileName = Path.Combine(targetDirectory, $"search-info_{request.FromSequenceNumber}-{request.MessageCount}_failed-searches.csv");
                var failedSearchesCsv = new StringBuilder("request ID,failure info\n");
                failedSearchResults.ForEach(r => failedSearchesCsv.AppendLine($"{r.SearchRequestId},{r.FailureInfo}"));
                await File.WriteAllTextAsync(failedSearchesFileName, failedSearchesCsv.ToString());
            }

            return (performanceInfoFileName, failedSearchesFileName);
        }
    }
}
