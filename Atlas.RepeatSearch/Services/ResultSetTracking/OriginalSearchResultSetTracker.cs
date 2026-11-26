using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.RepeatSearch.Data.Settings;
using Atlas.RepeatSearch.Services.Search;

namespace Atlas.RepeatSearch.Services.ResultSetTracking
{
    public interface IOriginalSearchResultSetTracker
    {
        Task StoreOriginalSearchResults(MatchingResultsNotification notification);
        Task ApplySearchResultDiff(string searchRequestId, SearchResultDifferential searchResultDifferential);
    }

    /// <summary>
    /// Only the donor code of a matching result. Json.NET skips the other properties, so the scoring data and the donor HLA
    /// of a results file are never held in memory.
    /// </summary>
    internal class DonorCodeOnlyResult
    {
        public string DonorCode { get; set; }
    }

    internal class OriginalSearchResultSetTracker : IOriginalSearchResultSetTracker
    {
        /// <summary>
        /// Kept out of sampling by <see cref="RepeatSearchEventsTelemetryInitializer"/>.
        /// </summary>
        internal const string StoredEventName = "Original search results stored";

        private readonly IBlobDownloader blobDownloader;
        private readonly ICanonicalResultSetRepository canonicalResultSetRepository;
        private readonly IAtlasLogger logger;
        private readonly int chunkSize;

        public OriginalSearchResultSetTracker(
            IBlobDownloader blobDownloader,
            ICanonicalResultSetRepository canonicalResultSetRepository,
            StoreOriginalSearchResultsBulkCopySettings settings,
            IAtlasLogger logger)
        {
            this.blobDownloader = blobDownloader;
            this.canonicalResultSetRepository = canonicalResultSetRepository;
            this.logger = logger;
            // Without the setting, all codes go in one chunk, as before chunks were added.
            chunkSize = settings.ChunkSize > 0 ? settings.ChunkSize : int.MaxValue;
        }

        public async Task StoreOriginalSearchResults(MatchingResultsNotification notification)
        {
            var searchRequestId = notification.SearchRequestId;
            var storeProperties = new Dictionary<string, string>
            {
                {"SearchRequestId", searchRequestId},
                {"NumberOfResults", notification.NumberOfResults?.ToString(CultureInfo.InvariantCulture)},
                {"ResultsBatched", notification.ResultsBatched.ToString()}
            };

            // SearchRequestTelemetryInitializer adds the search id to all telemetry of this store, for example its SQL dependencies.
            SearchRequestContext.SearchRequestId = searchRequestId;
            logger.SendTrace($"Storing the original search results of search {searchRequestId}.", LogLevel.Info, storeProperties);

            try
            {
                await StoreResultSet(notification);
            }
            catch (Exception exception)
            {
                // The host logs the failure of the function outside this async flow, so without the search id. This log has it, and
                // the size of the search, for example for a SQL timeout or a lost claim.
                logger.SendException(exception, LogLevel.Error, storeProperties);
                throw;
            }
        }

        private async Task StoreResultSet(MatchingResultsNotification notification)
        {
            var stopwatch = Stopwatch.StartNew();
            var searchRequestId = notification.SearchRequestId;

            var claim = await canonicalResultSetRepository.StartStoringResultSet(searchRequestId);
            if (claim == null)
            {
                // A repeat delivery of the message, after the set was stored.
                logger.SendTrace($"Original search results of search {searchRequestId} are already stored.");
                return;
            }

            var donorCount = 0;
            var chunkCount = 0;

            if (notification.NumberOfResults > 0)
            {
                var chunk = new List<string>();
                await foreach (var donorCode in ReadDonorCodes(notification))
                {
                    chunk.Add(donorCode);
                    if (chunk.Count == chunkSize)
                    {
                        await AddChunk();
                    }
                }

                if (chunk.Count > 0)
                {
                    await AddChunk();
                }

                async Task AddChunk()
                {
                    await canonicalResultSetRepository.AddResultsChunk(claim, chunk);
                    donorCount += chunk.Count;
                    chunkCount++;
                    chunk = new List<string>();
                }
            }

            // A short set would make repeat searches report donors of the original search as new, so it is never marked complete.
            if (donorCount != (notification.NumberOfResults ?? 0))
            {
                throw new InvalidOperationException(
                    $"Read {donorCount} donor codes from the results of search {searchRequestId}, but the notification has " +
                    $"{notification.NumberOfResults} results. The result set is left in progress.");
            }

            await canonicalResultSetRepository.CompleteResultSet(claim);

            logger.SendEvent(StoredEventName,
                props: new Dictionary<string, string>
                {
                    {"SearchRequestId", searchRequestId},
                    {"ResultsBatched", notification.ResultsBatched.ToString()},
                    {"TookOver", claim.TookOver.ToString()}
                },
                metrics: new Dictionary<string, double>
                {
                    {"DonorCount", donorCount},
                    {"ChunkCount", chunkCount},
                    {"ElapsedMs", stopwatch.ElapsedMilliseconds}
                });
        }

        public async Task ApplySearchResultDiff(string searchRequestId, SearchResultDifferential searchResultDifferential)
        {
            var donorCodesToAdd = searchResultDifferential.NewResults.Select(d => d.ExternalDonorCode).ToList();

            if (donorCodesToAdd.Any())
            {
                await canonicalResultSetRepository.AddResultsToSet(searchRequestId, donorCodesToAdd);
            }

            if (searchResultDifferential.RemovedResults.Any())
            {
                await canonicalResultSetRepository.RemoveResultsFromSet(searchRequestId, searchResultDifferential.RemovedResults);
            }
        }

        /// <summary>
        /// Reads only the donor codes: one batch file at a time for batched results, or as a stream for a single results file.
        /// </summary>
        private async IAsyncEnumerable<string> ReadDonorCodes(MatchingResultsNotification notification)
        {
            if (notification.ResultsBatched && !string.IsNullOrEmpty(notification.BatchFolderName))
            {
                var files = blobDownloader.DownloadFolderContentsFileByFile<DonorCodeOnlyResult>(
                    notification.BlobStorageContainerName, notification.BatchFolderName);

                await foreach (var results in files)
                {
                    foreach (var result in results)
                    {
                        yield return result.DonorCode;
                    }
                }

                yield break;
            }

            await using var stream = await blobDownloader.OpenRead(notification.BlobStorageContainerName, notification.ResultsFileName);
            await foreach (var donorCode in DonorCodeStreamReader.ReadDonorCodes(stream))
            {
                yield return donorCode;
            }
        }
    }
}
