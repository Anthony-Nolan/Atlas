namespace Atlas.Utilities.RerunFailedSearches
{
    /// <summary>
    /// One-time utility (ATL-158) that finds searches which failed after a given date and re-submits them
    /// directly onto the matching request topics with a configurable <c>ParallelMatchPrediction</c> value.
    /// Runs first-time searches and repeat searches as two independent passes.
    /// </summary>
    public class RerunFailedSearchesRunner
    {
        private readonly IFailedSearchNotificationReader notificationReader;
        private readonly IFailedSearchTrackingReader trackingReader;
        private readonly IFailedSearchResubmitter resubmitter;
        private readonly RerunSettings settings;
        private readonly TextWriter output;

        public RerunFailedSearchesRunner(
            IFailedSearchNotificationReader notificationReader,
            IFailedSearchTrackingReader trackingReader,
            IFailedSearchResubmitter resubmitter,
            RerunSettings settings,
            TextWriter output)
        {
            this.notificationReader = notificationReader;
            this.trackingReader = trackingReader;
            this.resubmitter = resubmitter;
            this.settings = settings;
            this.output = output;
        }

        public async Task Run(RerunInputs inputs)
        {
            output.WriteLine(
                $"Rerun failed searches — from {inputs.FromDateUtc:o}; " +
                $"onlyParallelFailures={inputs.OnlyReplayMatchPredictionParallelFailures}; " +
                $"forcedParallel={inputs.ForcedParallelMatchPredictionInRequest}");

            await RunSearches(inputs);
            await RunRepeatSearches(inputs);
        }

        private async Task RunSearches(RerunInputs inputs)
        {
            var failed = await notificationReader.GetFailedSince(
                settings.SearchResultsTopic, settings.ResultsAuditSubscription, inputs.FromDateUtc);
            output.WriteLine($"[Searches] Failed notifications since {inputs.FromDateUtc:o}: {failed.Count}");

            var identifiers = ParseDistinctGuids(failed.Select(f => f.SearchRequestId));
            var tracked = await trackingReader.GetSearches(identifiers, inputs.OnlyReplayMatchPredictionParallelFailures);
            output.WriteLine($"[Searches] Matching tracked searches to re-run: {tracked.Count}");

            foreach (var search in tracked)
            {
                try
                {
                    output.WriteLine($"[Searches] re-submitting {search.SearchIdentifier}");
                    await resubmitter.ResubmitSearch(
                        search.SearchIdentifier.ToString(), search.RequestJson, inputs.ForcedParallelMatchPredictionInRequest);
                }
                catch (Exception ex)
                {
                    output.WriteLine($"[Searches] FAILED to re-submit {search.SearchIdentifier}: {ex.Message}");
                }
            }
        }

        private async Task RunRepeatSearches(RerunInputs inputs)
        {
            var failed = await notificationReader.GetFailedSince(
                settings.RepeatSearchResultsTopic, settings.ResultsAuditSubscription, inputs.FromDateUtc);
            output.WriteLine($"[RepeatSearches] Failed notifications since {inputs.FromDateUtc:o}: {failed.Count}");

            // For a repeat search: SearchIdentifier == notification.RepeatSearchRequestId,
            // OriginalSearchIdentifier == notification.SearchRequestId.
            var identifiers = failed
                .Select(f => (Repeat: f.RepeatSearchRequestId, Original: f.SearchRequestId))
                .Where(pair => Guid.TryParse(pair.Repeat, out _) && Guid.TryParse(pair.Original, out _))
                .Select(pair => new RepeatSearchIdentifiers(Guid.Parse(pair.Repeat!), Guid.Parse(pair.Original)))
                .Distinct()
                .ToList();

            var tracked = await trackingReader.GetRepeatSearches(identifiers, inputs.OnlyReplayMatchPredictionParallelFailures);
            output.WriteLine($"[RepeatSearches] Matching tracked repeat searches to re-run: {tracked.Count}");

            foreach (var search in tracked)
            {
                try
                {
                    var originalSearchId = search.OriginalSearchIdentifier.Value.ToString();
                    output.WriteLine($"[RepeatSearches] re-submitting repeat {search.SearchIdentifier} (original {originalSearchId})");
                    await resubmitter.ResubmitRepeatSearch(
                        search.SearchIdentifier.ToString(), originalSearchId, search.RequestJson, inputs.ForcedParallelMatchPredictionInRequest);
                }
                catch (Exception ex)
                {
                    output.WriteLine($"[RepeatSearches] FAILED to re-submit repeat {search.SearchIdentifier}: {ex.Message}");
                }
            }
        }

        private static IReadOnlyCollection<Guid> ParseDistinctGuids(IEnumerable<string?> raw) =>
            raw.Where(id => Guid.TryParse(id, out _)).Select(id => Guid.Parse(id!)).Distinct().ToList();
    }
}
