namespace Atlas.Utilities.RerunFailedSearches
{
    /// <summary>
    /// Per-run inputs supplied on the command line.
    /// </summary>
    public class RerunInputs
    {
        /// <summary>
        /// Only searches whose results notification was enqueued strictly after this UTC instant are considered.
        /// Note the results topics' <c>audit</c> subscription retains messages for 14 days, so anything older
        /// than that will already have expired and cannot be found.
        /// </summary>
        public DateTimeOffset FromDateUtc { get; set; }

        /// <summary>
        /// When <c>true</c>, restricts the re-run set to searches whose match-prediction ran on the parallel
        /// ("Containers") path and was unsuccessful
        /// (<c>SearchRequestMatchPredictions.IsParallelMatchPrediction = 1 AND IsSuccessful = 0</c>).
        /// When <c>false</c>, every failed search found on the topic is re-run.
        /// </summary>
        public bool OnlyReplayMatchPredictionParallelFailures { get; set; }

        /// <summary>
        /// The value written to <c>SearchRequest.ParallelMatchPrediction</c> on each re-submitted request.
        /// </summary>
        public bool ForcedParallelMatchPredictionInRequest { get; set; }

        /// <summary>
        /// Parses command-line arguments of the form:
        /// <c>--from 2026-07-01 [--only-parallel-failures] [--forced-parallel true|false]</c>.
        /// </summary>
        public static RerunInputs Parse(string[] args)
        {
            var lookup = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--", StringComparison.Ordinal))
                {
                    continue;
                }

                var key = args[i][2..];
                var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
                lookup[key] = hasValue ? args[++i] : null;
            }

            if (!lookup.TryGetValue("from", out var fromRaw) || string.IsNullOrWhiteSpace(fromRaw))
            {
                throw new ArgumentException(
                    "Required argument --from <UTC date/time> is missing. " +
                    "Example: --from 2026-07-01 --forced-parallel false --only-parallel-failures");
            }

            return new RerunInputs
            {
                FromDateUtc = ParseUtc(fromRaw),
                OnlyReplayMatchPredictionParallelFailures = FlagIsSet(lookup, "only-parallel-failures"),
                ForcedParallelMatchPredictionInRequest = FlagIsSet(lookup, "forced-parallel")
            };
        }

        private static DateTimeOffset ParseUtc(string raw) =>
            DateTimeOffset.Parse(
                raw,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);

        // A bare flag (e.g. "--only-parallel-failures") is present-with-null and means true;
        // an explicit "--forced-parallel false" is honoured too.
        private static bool FlagIsSet(IReadOnlyDictionary<string, string?> lookup, string key) =>
            lookup.TryGetValue(key, out var value) && (value is null || bool.Parse(value));
    }
}
