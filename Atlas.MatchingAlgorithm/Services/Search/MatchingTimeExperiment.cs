using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    /// <summary>ATL-526 experiment: how matching runs a search. Experiment only, not for release.</summary>
    public enum MatchingTimeExperimentMode
    {
        /// <summary>Today's code.</summary>
        Normal,

        /// <summary>Runs every sub-search once (no scoring, no ATL-502 second pass), counts the donors and returns no results.</summary>
        MatchingOnly,

        /// <summary>Runs matching and scoring (with the ATL-502 second pass), counts the donors and returns no results.</summary>
        MatchingAndScoring
    }

    /// <summary>
    /// ATL-526 experiment: measures how long matching takes for very large (MMUD-size) searches on DEV. Experiment only, not for release.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The mode comes from the environment variable (app setting) <c>ATL526_MATCHING_MODE</c>, so it can be changed on DEV
    /// without a deploy. Not set, or not a known value, means <see cref="MatchingTimeExperimentMode.Normal"/>.
    /// </para>
    /// <para>
    /// In the experiment modes the search returns 0 results, so the rest of the pipeline (results file, match prediction,
    /// repeat search) sees a normal empty search and does not hold millions of results in memory. A failure is recorded in
    /// the event and also ends as an empty search, and a redelivered message (attempt 2 or later) is not run again, so one
    /// long failed attempt is not retried 10 times.
    /// </para>
    /// <para>The event holds no patient HLA and no exception message: only the shape of the request and the exception type.</para>
    /// </remarks>
    public static class MatchingTimeExperiment
    {
        public const string ModeSettingName = "ATL526_MATCHING_MODE";
        public const string EventName = "MMUD matching timings";
        private static readonly TimeSpan MemorySampleInterval = TimeSpan.FromMilliseconds(500);

        /// <summary>Set by tests only. When null, the mode comes from the app setting.</summary>
        internal static MatchingTimeExperimentMode? ModeOverride { get; set; }

        public static MatchingTimeExperimentMode Mode =>
            ModeOverride
            ?? (Enum.TryParse<MatchingTimeExperimentMode>(Environment.GetEnvironmentVariable(ModeSettingName), true, out var mode)
                ? mode
                : MatchingTimeExperimentMode.Normal);

        public static bool IsOn => Mode != MatchingTimeExperimentMode.Normal;

        /// <summary>
        /// Runs the search in the given experiment mode, sends one unsampled event and returns no results.
        /// <paramref name="scoredMatches"/> is only called in <see cref="MatchingTimeExperimentMode.MatchingAndScoring"/>.
        /// </summary>
        internal static async Task Run(
            MatchingTimeExperimentMode mode,
            SearchRequest request,
            int subSearchCount,
            IAsyncEnumerable<MatchResult> matches,
            Func<IAsyncEnumerable<MatchResult>, Task<IEnumerable<MatchAndScoreResult>>> scoredMatches,
            IAtlasLogger logger)
        {
            var start = Stopwatch.GetTimestamp();
            var allocatedAtStart = GC.GetTotalAllocatedBytes();
            var gen2AtStart = GC.CollectionCount(2);
            var firstDonorMs = -1d;
            var matchingEndMs = -1d;
            long rows = 0;
            var donorIds = new HashSet<int>();
            Exception failure = null;

            using var memory = new PeakMemorySampler(MemorySampleInterval);

            async IAsyncEnumerable<MatchResult> Counted()
            {
                await foreach (var match in matches)
                {
                    if (rows++ == 0)
                    {
                        firstDonorMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    }

                    donorIds.Add(match.DonorId);
                    yield return match;
                }

                matchingEndMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            }

            long scoredDonors = 0;
            try
            {
                if (mode == MatchingTimeExperimentMode.MatchingAndScoring)
                {
                    // ATL-502: scoring counts the stream first, so matching runs twice here, and "rows" counts both runs.
                    // The "Matching timings" events (Run 1 and Run 2) give each run on its own.
                    var scored = await scoredMatches(Counted());
                    scoredDonors = scored.Select(m => m.MatchResult.DonorId).Distinct().LongCount();
                }
                else
                {
                    await foreach (var _ in Counted())
                    {
                    }
                }
            }
            catch (Exception e)
            {
                failure = e;
            }

            var totalMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            memory.Stop();

            var metrics = new Dictionary<string, double>
            {
                ["TotalMs"] = totalMs,
                ["FirstDonorMs"] = firstDonorMs,
                ["MatchingEndMs"] = matchingEndMs,
                ["Rows"] = rows,
                ["DistinctDonors"] = donorIds.Count,
                ["ScoredDonors"] = scoredDonors,
                ["SubSearchCount"] = subSearchCount,
                ["DonorMismatchCount"] = request.MatchCriteria?.DonorMismatchCount ?? -1,
                ["PeakWorkingSetMb"] = memory.PeakWorkingSetBytes / 1024d / 1024d,
                ["PeakGcHeapMb"] = memory.PeakGcHeapBytes / 1024d / 1024d,
                ["AllocatedMb"] = (GC.GetTotalAllocatedBytes() - allocatedAtStart) / 1024d / 1024d,
                ["Gen2Collections"] = GC.CollectionCount(2) - gen2AtStart,
            };

            var properties = DescribeRequest(request);
            properties["Mode"] = mode.ToString();
            properties["DonorIdRange"] = DonorIdRangeExperiment.Current?.ToString() ?? "all";
            properties["Instance"] = InstanceName();
            properties["Outcome"] = failure == null ? "Completed" : "Failed";
            if (failure != null)
            {
                // The type only: exception messages are not sent to App Insights (ATL-260 rule).
                properties["ExceptionType"] = failure.GetType().FullName;
            }

            logger.SendEvent(EventName, LogLevel.Info, properties, metrics);
        }

        /// <summary>Sends the event for a redelivered message (attempt 2 or later), which the experiment does not run again.</summary>
        public static void SendSkippedRetry(SearchRequest request, int attemptNumber, IAtlasLogger logger)
        {
            var properties = DescribeRequest(request);
            properties["Mode"] = Mode.ToString();
            properties["Instance"] = InstanceName();
            properties["Outcome"] = "SkippedRetry";

            logger.SendEvent(EventName, LogLevel.Info, properties, new Dictionary<string, double> { ["AttemptNumber"] = attemptNumber });
        }

        /// <summary>The shape of the request only. No patient HLA.</summary>
        private static Dictionary<string, string> DescribeRequest(SearchRequest request)
        {
            var loci = request.MatchCriteria?.LocusMismatchCriteria;
            var perLocus = new (string Locus, int? Count)[]
                {
                    ("A", loci?.A), ("B", loci?.B), ("C", loci?.C), ("Dqb1", loci?.Dqb1), ("Drb1", loci?.Drb1)
                }
                .Where(x => x.Count != null)
                .Select(x => $"{x.Locus}{x.Count}");

            return new Dictionary<string, string>
            {
                ["SearchDonorType"] = request.SearchDonorType.ToString(),
                ["LocusMismatches"] = string.Join(",", perLocus),
                ["RegistryCodeCount"] = (request.DonorRegistryCodes?.Count ?? 0).ToString(),
                ["RunMatchPrediction"] = request.RunMatchPrediction.ToString(),
            };
        }

        private static string InstanceName() =>
            Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") is { Length: > 0 } instance
                ? instance
                : Environment.MachineName;

        /// <summary>Samples the process working set and the GC heap size in the background and keeps the largest values.</summary>
        private sealed class PeakMemorySampler : IDisposable
        {
            private readonly Timer timer;
            private long peakWorkingSetBytes;
            private long peakGcHeapBytes;

            public PeakMemorySampler(TimeSpan interval)
            {
                Sample(null);
                timer = new Timer(Sample, null, interval, interval);
            }

            public long PeakWorkingSetBytes => Interlocked.Read(ref peakWorkingSetBytes);
            public long PeakGcHeapBytes => Interlocked.Read(ref peakGcHeapBytes);

            public void Stop()
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                Sample(null);
            }

            public void Dispose() => timer.Dispose();

            private void Sample(object _)
            {
                UpdateMax(ref peakWorkingSetBytes, Environment.WorkingSet);
                UpdateMax(ref peakGcHeapBytes, GC.GetGCMemoryInfo().HeapSizeBytes);
            }

            private static void UpdateMax(ref long target, long value)
            {
                var current = Interlocked.Read(ref target);
                while (value > current)
                {
                    var previous = Interlocked.CompareExchange(ref target, value, current);
                    if (previous == current)
                    {
                        return;
                    }

                    current = previous;
                }
            }
        }
    }
}
