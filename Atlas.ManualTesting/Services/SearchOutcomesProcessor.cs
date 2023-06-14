using Atlas.Client.Models.Search.Results.LogFile;
using Atlas.Common.AzureStorage.Blob;
using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.ManualTesting.Services
{
    public interface ISearchOutcomesProcessor
    {
        Task<(string PerformanceInfoFileName, string FailedSearchesFileName, string ProcessingErrorsFileName)> ProcessSearchMessages(SearchOutcomesPeekRequest request);
    }

    internal class SearchOutcomesProcessor : ISearchOutcomesProcessor
    {
        private readonly AzureStorageSettings azureStorageSettings;
        private readonly ISearchResultNotificationsPeeker notificationsPeeker;
        private readonly IBlobDownloader blobDownloader;

        public SearchOutcomesProcessor(ISearchResultNotificationsPeeker notificationsPeeker, IOptions<AzureStorageSettings> azureStorageSettings, IBlobDownloader blobDownloader)
        {
            this.azureStorageSettings = azureStorageSettings.Value;
            this.notificationsPeeker = notificationsPeeker;
            this.blobDownloader = blobDownloader;
        }

        public async Task<(string PerformanceInfoFileName, string FailedSearchesFileName, string ProcessingErrorsFileName)> ProcessSearchMessages(SearchOutcomesPeekRequest request)
        {
            var notifications = await notificationsPeeker.GetSearchResultsNotifications(request);

            var performanceInfoResults = new List<SearchPerformanceInfo>();
            var failedSearchResults = new List<FailedSearch>();
            var processingErrors = new List<SearchOutcomesProcessingError>();

            foreach (var notification in notifications.PeekedNotifications)
            {
                var resultItem = new SearchPerformanceInfo
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
                    await SetMatchingPerformanceInfo(resultItem, logFilename);
                }
                catch (Exception ex)
                {
                    processingErrors.Add(BuildProcessingError(azureStorageSettings.MatchingAlgorithmResultsBlobContainer, logFilename, ex.Message));
                }

                try
                {
                    await SetMatchPredictionPerformanceInfo(resultItem, logFilename);
                }
                catch (Exception ex)
                {
                    processingErrors.Add(BuildProcessingError(azureStorageSettings.SearchResultsBlobContainer, logFilename, ex.Message));
                }

                performanceInfoResults.Add(resultItem);
            }

            return await SaveResults( request, performanceInfoResults, failedSearchResults, processingErrors);
        }

        private async Task SetMatchingPerformanceInfo(SearchPerformanceInfo searchPerformanceInfo, string logFilename)
        {
            var matchingAlgorithmResultsLog = await blobDownloader.Download<SearchLog>(azureStorageSettings.MatchingAlgorithmResultsBlobContainer, logFilename);
            searchPerformanceInfo.MatchingQueueDuration = matchingAlgorithmResultsLog.RequestPerformanceMetrics.StartTime - matchingAlgorithmResultsLog.RequestPerformanceMetrics.InitiationTime;
            searchPerformanceInfo.MatchingRequestDuration = matchingAlgorithmResultsLog.RequestPerformanceMetrics.Duration;
            searchPerformanceInfo.MatchingInitiationTime = matchingAlgorithmResultsLog.RequestPerformanceMetrics.InitiationTime;
        }

        private async Task SetMatchPredictionPerformanceInfo(SearchPerformanceInfo searchPerformanceInfo, string logFilename)
        {
            var searchResultsLog = await blobDownloader.Download<SearchLog>(azureStorageSettings.SearchResultsBlobContainer, logFilename);
            searchPerformanceInfo.MatchPredictionQueueDuration = searchResultsLog.RequestPerformanceMetrics.StartTime - searchResultsLog.RequestPerformanceMetrics.InitiationTime;
            searchPerformanceInfo.MatchPredictionRequestDuration = searchResultsLog.RequestPerformanceMetrics.Duration;
            searchPerformanceInfo.MatchPredictionCompletionTime = searchResultsLog.RequestPerformanceMetrics.CompletionTime;
        }

        private async Task<(string PerformanceInfoFileName, string FailedSearchesFileName, string ProcessingErrorsFileName)> SaveResults(SearchOutcomesPeekRequest request,
            List<SearchPerformanceInfo> performanceInfoResults, List<FailedSearch> failedSearchResults, List<SearchOutcomesProcessingError> processingErrors)
        {
            var targetDirectory = request.Directory ?? Directory.GetCurrentDirectory();
            return (await WritePerformanceInfo(request, performanceInfoResults, targetDirectory)
                , await WriteFailureInfo(request, failedSearchResults, targetDirectory)
                , await WriteProcessingErrors(request, processingErrors, targetDirectory));
        }

        private async Task<string> WritePerformanceInfo(SearchOutcomesPeekRequest request, List<SearchPerformanceInfo> performanceInfoResults, string targetDirectory)
        {
            if (!performanceInfoResults.Any())
                return null;

            var performanceInfoFileName = Path.Combine(targetDirectory, $"search-info_{request.FromSequenceNumber}-{request.MessageCount}_performance-info.csv");

            if (!Directory.Exists(targetDirectory))
                Directory.CreateDirectory(targetDirectory);

            var performanceInfoCsv = new StringBuilder(
                "request ID,was successful,matching queue duration,matching request duration,match prediction queue duration,match predition request duration,matching initiation time,match prediction completion time,donor count\n");
            performanceInfoResults.ForEach(r => performanceInfoCsv.AppendLine(
                $"{r.SearchRequestId},{r.WasSuccessful},{r.MatchingQueueDuration},{r.MatchingRequestDuration},{r.MatchPredictionQueueDuration},{r.MatchPredictionRequestDuration},{r.MatchingInitiationTime},{r.MatchPredictionCompletionTime},{r.DonorCount}"));
            await File.WriteAllTextAsync(performanceInfoFileName, performanceInfoCsv.ToString());

            return performanceInfoFileName;
        }

        private async Task<string> WriteFailureInfo(SearchOutcomesPeekRequest request, List<FailedSearch> failedSearchResults, string targetDirectory)
        {
            if (!failedSearchResults.Any())
                return null;

            var failedSearchesFileName = Path.Combine(targetDirectory, $"search-info_{request.FromSequenceNumber}-{request.MessageCount}_failed-searches.csv");
            var failedSearchesCsv = new StringBuilder("request ID,failure info\n");
            failedSearchResults.ForEach(r => failedSearchesCsv.AppendLine($"{r.SearchRequestId},{r.FailureInfo}"));
            await File.WriteAllTextAsync(failedSearchesFileName, failedSearchesCsv.ToString());

            return failedSearchesFileName;
        }

        private async Task<string> WriteProcessingErrors(SearchOutcomesPeekRequest request, List<SearchOutcomesProcessingError> processingErrors, string targetDirectory)
        {
            if (!processingErrors.Any())
                return null;

            var failedSearchesFileName = Path.Combine(targetDirectory, $"search-info_{request.FromSequenceNumber}-{request.MessageCount}_file-errors.csv");
            var failedSearchesCsv = new StringBuilder("blob container,log file name, exception message\n");
            processingErrors.ForEach(r => failedSearchesCsv.AppendLine($"{r.BlobContainer},{r.LogFileName},{r.ExceptionMessage}"));
            await File.WriteAllTextAsync(failedSearchesFileName, failedSearchesCsv.ToString());

            return failedSearchesFileName;
        }

        private SearchOutcomesProcessingError BuildProcessingError(string blobContainer, string logFileName, string exceptionMessage)
            => new()
            {
                BlobContainer = blobContainer,
                LogFileName = logFileName,
                ExceptionMessage = JsonConvert.SerializeObject(exceptionMessage)
            };
    }
}
