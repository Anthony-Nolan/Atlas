using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.ApplicationInsights;
using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.Validation;
using Atlas.ManualTesting.Models;
using Atlas.SearchTracking.Data.Context;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TrackedSearchRequest = Atlas.SearchTracking.Data.Models.SearchRequest;

namespace Atlas.ManualTesting.Services
{
    public interface IFailedParallelSearchReplayer
    {
        /// <summary>
        /// Finds searches recorded in <c>[SearchTracking].[SearchRequests]</c> that ran on the parallel
        /// ("Containers") match-prediction path and did not send results (i.e. failed or are still incomplete),
        /// and — unless this is a dry run — re-dispatches each one with <c>ParallelMatchPrediction = false</c>.
        /// </summary>
        Task<ParallelMatchPredictionReplayResponse> ReplayFailedParallelSearches(ParallelMatchPredictionReplayRequest request);
    }

    /// <summary>
    /// One-time utility (ATL-158) for replaying failed/incomplete parallel match-prediction searches down the legacy
    /// sequential path. It re-dispatches through the live Public API (via <see cref="IPublicApiFunctionsClient"/>) so
    /// replays undergo the same validation, tracking and routing as any other search.
    /// </summary>
    public class FailedParallelSearchReplayer : IFailedParallelSearchReplayer
    {
        private readonly ISearchTrackingContext searchTrackingContext;
        private readonly IPublicApiFunctionsClient publicApiClient;
        private readonly IAtlasLogger logger;

        public FailedParallelSearchReplayer(
            ISearchTrackingContext searchTrackingContext,
            IPublicApiFunctionsClient publicApiClient,
            IAtlasLogger logger)
        {
            this.searchTrackingContext = searchTrackingContext;
            this.publicApiClient = publicApiClient;
            this.logger = logger;
        }

        public async Task<ParallelMatchPredictionReplayResponse> ReplayFailedParallelSearches(ParallelMatchPredictionReplayRequest request)
        {
            var candidates = await GetFailedOrIncompleteParallelSearches(request.FromRequestTimeUtc, request.ToRequestTimeUtc);

            var response = new ParallelMatchPredictionReplayResponse
            {
                DryRun = request.DryRun,
                CandidateCount = candidates.Count,
                Candidates = candidates.Select(ToCandidate).ToList(),
                Replays = Array.Empty<ParallelMatchPredictionReplayOutcome>()
            };

            if (request.DryRun)
            {
                logger.SendTrace(
                    $"Parallel match prediction replay dry run: found {candidates.Count} failed/incomplete parallel " +
                    $"search(es) requested between {request.FromRequestTimeUtc:o} and {request.ToRequestTimeUtc:o}. " +
                    "No searches were re-dispatched.");
                return response;
            }

            var toReplay = candidates;
            if (request.SearchIdentifiers is { Count: > 0 })
            {
                var allowList = request.SearchIdentifiers.ToHashSet();
                toReplay = candidates.Where(c => allowList.Contains(c.SearchIdentifier)).ToList();
            }

            var outcomes = new List<ParallelMatchPredictionReplayOutcome>(toReplay.Count);
            foreach (var candidate in toReplay)
            {
                outcomes.Add(await Replay(candidate));
            }

            response.Replays = outcomes;
            response.ReplayedCount = outcomes.Count(o => o.WasReplayed);
            response.FailedToReplayCount = outcomes.Count(o => !o.WasReplayed);

            logger.SendTrace(
                $"Parallel match prediction replay complete: {response.ReplayedCount} re-dispatched, " +
                $"{response.FailedToReplayCount} failed, out of {toReplay.Count} attempted.");

            return response;
        }

        /// <remarks>
        /// A search qualifies when it has a linked match-prediction record flagged <c>IsParallelMatchPrediction</c>
        /// (i.e. it was dispatched with <c>ParallelMatchPrediction = true</c>) and no results were sent
        /// (<c>ResultsSent != true</c>), which covers both failed and still-incomplete runs. The time window guards
        /// against picking up genuinely in-flight searches; combine with a dry run to review before re-dispatching.
        /// </remarks>
        private async Task<List<TrackedSearchRequest>> GetFailedOrIncompleteParallelSearches(DateTime from, DateTime to)
        {
            return await searchTrackingContext.SearchRequests
                .Include(x => x.MatchPrediction)
                .Where(x => x.MatchPrediction != null
                            && x.MatchPrediction.IsParallelMatchPrediction
                            && x.ResultsSent != true
                            && x.RequestTimeUtc >= from
                            && x.RequestTimeUtc <= to)
                .OrderBy(x => x.RequestTimeUtc)
                .ToListAsync();
        }

        private async Task<ParallelMatchPredictionReplayOutcome> Replay(TrackedSearchRequest tracked)
        {
            var outcome = new ParallelMatchPredictionReplayOutcome
            {
                OriginalSearchIdentifier = tracked.SearchIdentifier,
                IsRepeatSearch = tracked.IsRepeatSearch
            };

            try
            {
                if (tracked.IsRepeatSearch)
                {
                    // Repeat-search tracking stores the whole RepeatSearchRequest (see RepeatSearchDispatcher).
                    var repeatRequest = JsonConvert.DeserializeObject<RepeatSearchRequest>(tracked.RequestJson);
                    repeatRequest.SearchRequest.ParallelMatchPrediction = false;
                    ApplyResult(outcome, await publicApiClient.PostRepeatSearchRequest(repeatRequest));
                }
                else
                {
                    // Regular-search tracking stores the SearchRequest itself (see SearchDispatcher).
                    var searchRequest = JsonConvert.DeserializeObject<SearchRequest>(tracked.RequestJson);
                    searchRequest.ParallelMatchPrediction = false;
                    ApplyResult(outcome, await publicApiClient.PostSearchRequest(searchRequest));
                }
            }
            catch (Exception ex)
            {
                outcome.WasReplayed = false;
                outcome.Error = ex.Message;
                logger.SendTrace($"Failed to replay parallel search {tracked.SearchIdentifier}: {ex.Message}", LogLevel.Error);
            }

            return outcome;
        }

        private static void ApplyResult(
            ParallelMatchPredictionReplayOutcome outcome,
            ResponseFromValidatedRequest<SearchInitiationResponse> result)
        {
            if (result.WasSuccess)
            {
                outcome.WasReplayed = true;
                outcome.NewSearchIdentifier = outcome.IsRepeatSearch
                    ? result.ResponseOnSuccess.RepeatSearchIdentifier
                    : result.ResponseOnSuccess.SearchIdentifier;
            }
            else
            {
                outcome.WasReplayed = false;
                outcome.Error = "Request validation failed: " + JsonConvert.SerializeObject(result.ValidationFailures);
            }
        }

        private static ParallelMatchPredictionReplayCandidate ToCandidate(TrackedSearchRequest x) => new()
        {
            SearchIdentifier = x.SearchIdentifier,
            IsRepeatSearch = x.IsRepeatSearch,
            OriginalSearchIdentifier = x.OriginalSearchIdentifier,
            RequestTimeUtc = x.RequestTimeUtc,
            MatchPredictionIsSuccessful = x.MatchPrediction?.IsSuccessful,
            MatchPredictionFailureType = x.MatchPrediction?.FailureInfo_Type?.ToString()
        };
    }
}
