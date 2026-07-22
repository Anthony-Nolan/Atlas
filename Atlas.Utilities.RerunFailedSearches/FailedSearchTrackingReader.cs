using Atlas.SearchTracking.Data.Context;
using Microsoft.EntityFrameworkCore;
using TrackedSearchRequest = Atlas.SearchTracking.Data.Models.SearchRequest;

namespace Atlas.Utilities.RerunFailedSearches
{
    /// <summary>Identifies a repeat search in <c>[SearchTracking].[SearchRequests]</c> by both of its keys.</summary>
    public record RepeatSearchIdentifiers(Guid SearchIdentifier, Guid OriginalSearchIdentifier);

    public interface IFailedSearchTrackingReader
    {
        /// <summary>
        /// First-time searches: <c>[SearchRequests]</c> rows filtered by <c>SearchIdentifier</c> and
        /// <c>IsRepeatSearch = 0</c>, ordered oldest first.
        /// </summary>
        Task<IReadOnlyList<TrackedSearchRequest>> GetSearches(
            IReadOnlyCollection<Guid> searchIdentifiers,
            bool onlyParallelMatchPredictionFailures);

        /// <summary>
        /// Repeat searches: <c>[SearchRequests]</c> rows filtered by <c>SearchIdentifier</c>,
        /// <c>OriginalSearchIdentifier</c> and <c>IsRepeatSearch = 1</c>, ordered oldest first.
        /// </summary>
        Task<IReadOnlyList<TrackedSearchRequest>> GetRepeatSearches(
            IReadOnlyCollection<RepeatSearchIdentifiers> identifiers,
            bool onlyParallelMatchPredictionFailures);
    }

    public class FailedSearchTrackingReader : IFailedSearchTrackingReader
    {
        private readonly Func<SearchTrackingContext> contextFactory;

        public FailedSearchTrackingReader(Func<SearchTrackingContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task<IReadOnlyList<TrackedSearchRequest>> GetSearches(
            IReadOnlyCollection<Guid> searchIdentifiers,
            bool onlyParallelMatchPredictionFailures)
        {
            if (searchIdentifiers.Count == 0)
            {
                return Array.Empty<TrackedSearchRequest>();
            }

            await using var context = contextFactory();

            var query = context.SearchRequests
                .Include(x => x.MatchPrediction)
                .Where(x => !x.IsRepeatSearch && searchIdentifiers.Contains(x.SearchIdentifier));

            query = ApplyParallelFailureFilter(query, onlyParallelMatchPredictionFailures);

            return await query.OrderBy(x => x.RequestTimeUtc).ToListAsync();
        }

        public async Task<IReadOnlyList<TrackedSearchRequest>> GetRepeatSearches(
            IReadOnlyCollection<RepeatSearchIdentifiers> identifiers,
            bool onlyParallelMatchPredictionFailures)
        {
            if (identifiers.Count == 0)
            {
                return Array.Empty<TrackedSearchRequest>();
            }

            var searchIdentifiers = identifiers.Select(i => i.SearchIdentifier).Distinct().ToList();
            var expectedPairs = identifiers.Select(i => (i.SearchIdentifier, i.OriginalSearchIdentifier)).ToHashSet();

            await using var context = contextFactory();

            var query = context.SearchRequests
                .Include(x => x.MatchPrediction)
                .Where(x => x.IsRepeatSearch && searchIdentifiers.Contains(x.SearchIdentifier));

            query = ApplyParallelFailureFilter(query, onlyParallelMatchPredictionFailures);

            var rows = await query.ToListAsync();

            // Enforce the OriginalSearchIdentifier half of the filter (nullable + composite, so matched in memory).
            return rows
                .Where(x => x.OriginalSearchIdentifier.HasValue
                            && expectedPairs.Contains((x.SearchIdentifier, x.OriginalSearchIdentifier.Value)))
                .OrderBy(x => x.RequestTimeUtc)
                .ToList();
        }

        private static IQueryable<TrackedSearchRequest> ApplyParallelFailureFilter(
            IQueryable<TrackedSearchRequest> query,
            bool onlyParallelMatchPredictionFailures) =>
            onlyParallelMatchPredictionFailures
                ? query.Where(x =>
                    x.MatchPrediction != null
                    && x.MatchPrediction.IsParallelMatchPrediction
                    && x.MatchPrediction.IsSuccessful == false)
                : query;
    }
}
