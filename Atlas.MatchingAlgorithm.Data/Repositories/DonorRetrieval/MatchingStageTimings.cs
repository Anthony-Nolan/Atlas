using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval
{
    /// <summary>
    /// Where one search spent its matching time (ATL-510: find the matching bottlenecks). Experiment only, not for release.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Begin"/> is called once per search, from the method that awaits the whole search, so the collection
    /// flows (through an <see cref="AsyncLocal{T}"/>) into every matching iterator the search pulls. Each run of one
    /// sub-search starts a <see cref="MatchingRunTimings"/> with <see cref="StartRun"/>. Sub-searches run one after the
    /// other, so the current run is a plain field. Because of ATL-502 every sub-search runs twice: run 1 is the
    /// <c>CountAsync</c> pass, run 2 is the pass that feeds scoring.
    /// </para>
    /// <para>All the <c>Add*</c> calls do nothing when no collection is running (for example, in tests).</para>
    /// </remarks>
    public sealed class MatchingStageTimings
    {
        private static readonly AsyncLocal<MatchingStageTimings> Current = new();

        private int runsStarted;
        private readonly int subSearchCount;

        private MatchingStageTimings(int subSearchCount)
        {
            this.subSearchCount = Math.Max(subSearchCount, 1);
        }

        /// <summary>The sub-search run in progress for the current search, or null.</summary>
        public static MatchingRunTimings Run => Current.Value?.CurrentRun;

        private MatchingRunTimings CurrentRun { get; set; }

        public static Collection Begin(int subSearchCount)
        {
            var previous = Current.Value;
            Current.Value = new MatchingStageTimings(subSearchCount);
            return new Collection(previous);
        }

        /// <summary>Starts timing one run of one sub-search. Returns null when no collection is running.</summary>
        public static MatchingRunTimings StartRun()
        {
            var timings = Current.Value;
            if (timings == null)
            {
                return null;
            }

            var index = timings.runsStarted++;
            var run = new MatchingRunTimings(index / timings.subSearchCount + 1, index % timings.subSearchCount, timings.subSearchCount);
            timings.CurrentRun = run;
            return run;
        }

        public readonly struct Collection : IDisposable
        {
            private readonly MatchingStageTimings previous;

            internal Collection(MatchingStageTimings previous)
            {
                this.previous = previous;
            }

            public void Dispose() => Current.Value = previous;
        }
    }

    /// <summary>Timings and counts for one run of one sub-search. Not thread safe: matching pulls one stream at a time.</summary>
    public sealed class MatchingRunTimings
    {
        private static readonly int LocusCount = Enum.GetValues<Locus>().Length;

        private readonly long startTimestamp = Stopwatch.GetTimestamp();
        private readonly long allocatedBytesAtStart = GC.GetTotalAllocatedBytes();
        private readonly int gen2AtStart = GC.CollectionCount(2);
        private readonly int runNumber;
        private readonly int subSearchIndex;
        private readonly int subSearchCount;
        private readonly List<Locus> lociOrder = new();

        private readonly long[] sqlCalls = new long[LocusCount];
        private readonly long[] forcedOrderCalls = new long[LocusCount];
        private readonly long[] connectionOpenTicks = new long[LocusCount];
        private readonly long[] tempTableTicks = new long[LocusCount];
        private readonly long[] sqlExecTicks = new long[LocusCount];
        private readonly long[] sqlReadTicks = new long[LocusCount];
        private readonly long[] rows = new long[LocusCount];
        private readonly long[] donorIdsSent = new long[LocusCount];
        private readonly long[] donorsOut = new long[LocusCount];
        private readonly long[] pGroupIdTicks = new long[LocusCount];
        private readonly long[] pGroupsPosition1 = new long[LocusCount];
        private readonly long[] pGroupsPosition2 = new long[LocusCount];
        private readonly long[] untypedTicks = new long[LocusCount];
        private readonly long[] untypedDonors = new long[LocusCount];
        private readonly long[] materialisedDonors = new long[LocusCount];

        private long consumerTicks;
        private long phase1Donors;
        private long phase2Donors;
        private long donorLoadTicks;
        private long donorLoadCalls;
        private long donorsLoaded;
        private long phase2FilterTicks;

        internal MatchingRunTimings(int runNumber, int subSearchIndex, int subSearchCount)
        {
            this.runNumber = runNumber;
            this.subSearchIndex = subSearchIndex;
            this.subSearchCount = subSearchCount;
        }

        public static long Now() => Stopwatch.GetTimestamp();
        private static long Since(long timestamp) => Stopwatch.GetElapsedTime(timestamp).Ticks;

        public void StartLocus(Locus locus) => lociOrder.Add(locus);
        public void AddConnectionOpen(Locus locus, long start) => connectionOpenTicks[(int)locus] += Since(start);
        public void AddTempTables(Locus locus, long start) => tempTableTicks[(int)locus] += Since(start);

        public void AddSqlExec(Locus locus, long start, int donorIds, bool forcedOrder = false)
        {
            sqlCalls[(int)locus]++;
            if (forcedOrder)
            {
                forcedOrderCalls[(int)locus]++;
            }

            sqlExecTicks[(int)locus] += Since(start);
            donorIdsSent[(int)locus] += donorIds;
        }

        public void AddSqlRead(Locus locus, long elapsedTicks, long rowCount)
        {
            sqlReadTicks[(int)locus] += elapsedTicks;
            rows[(int)locus] += rowCount;
        }

        public void AddPGroupIds(Locus locus, long start, int position1, int position2)
        {
            pGroupIdTicks[(int)locus] += Since(start);
            pGroupsPosition1[(int)locus] = position1;
            pGroupsPosition2[(int)locus] = position2;
        }

        public void AddUntyped(Locus locus, long start, int donors)
        {
            untypedTicks[(int)locus] += Since(start);
            untypedDonors[(int)locus] += donors;
        }

        public void CountDonorOut(Locus locus) => donorsOut[(int)locus]++;
        public void AddMaterialised(Locus locus, int donors) => materialisedDonors[(int)locus] += donors;
        public void AddConsumer(long start) => consumerTicks += Since(start);
        public void SetPhase1Donors(int count) => phase1Donors = count;
        public void SetPhase2Donors(int count) => phase2Donors = count;

        public void AddDonorLoad(long start, int donors)
        {
            donorLoadCalls++;
            donorLoadTicks += Since(start);
            donorsLoaded += donors;
        }

        public void AddPhase2Filter(long start) => phase2FilterTicks += Since(start);

        /// <summary>
        /// Sends the unsampled "Matching timings" event (the matching Functions app excludes events from sampling).
        /// <c>OwnMs</c> = <c>TotalMs</c> - <c>ConsumerMs</c>: the time matching spent, without the time its consumer
        /// (the ATL-502 count, or scoring) spent between two donors.
        /// </summary>
        public void Send(IAtlasLogger logger, IDictionary<string, string> extraProperties)
        {
            static double Ms(long ticks) => TimeSpan.FromTicks(ticks).TotalMilliseconds;

            var totalTicks = Since(startTimestamp);
            var metrics = new Dictionary<string, double>
            {
                ["TotalMs"] = Ms(totalTicks),
                ["ConsumerMs"] = Ms(consumerTicks),
                ["OwnMs"] = Ms(totalTicks - consumerTicks),
                ["Phase1Donors"] = phase1Donors,
                ["Phase2Donors"] = phase2Donors,
                ["DonorLoadMs"] = Ms(donorLoadTicks),
                ["DonorLoadCalls"] = donorLoadCalls,
                ["DonorsLoaded"] = donorsLoaded,
                ["Phase2FilterMs"] = Ms(phase2FilterTicks),
                ["ConnectionOpenMs"] = Ms(connectionOpenTicks.Sum()),
                ["TempTableMs"] = Ms(tempTableTicks.Sum()),
                ["SqlExecMs"] = Ms(sqlExecTicks.Sum()),
                ["SqlReadMs"] = Ms(sqlReadTicks.Sum()),
                ["PGroupIdMs"] = Ms(pGroupIdTicks.Sum()),
                ["UntypedMs"] = Ms(untypedTicks.Sum()),
                ["SqlCalls"] = sqlCalls.Sum(),
                ["ForcedOrderCalls"] = forcedOrderCalls.Sum(),
                ["ForcedOrderMinDonorIds"] = DonorSearchRepository.ForcedOrderMinDonorIds,
                ["Rows"] = rows.Sum(),
                ["AllocatedMb"] = (GC.GetTotalAllocatedBytes() - allocatedBytesAtStart) / 1024d / 1024d,
                ["Gen2Collections"] = GC.CollectionCount(2) - gen2AtStart,
                ["WorkingSetMb"] = Environment.WorkingSet / 1024d / 1024d,
            };

            foreach (var locus in lociOrder.Distinct())
            {
                var i = (int)locus;
                metrics[$"{locus}SqlCalls"] = sqlCalls[i];
                metrics[$"{locus}ForcedOrderCalls"] = forcedOrderCalls[i];
                metrics[$"{locus}ConnectionOpenMs"] = Ms(connectionOpenTicks[i]);
                metrics[$"{locus}TempTableMs"] = Ms(tempTableTicks[i]);
                metrics[$"{locus}SqlExecMs"] = Ms(sqlExecTicks[i]);
                metrics[$"{locus}SqlReadMs"] = Ms(sqlReadTicks[i]);
                metrics[$"{locus}Rows"] = rows[i];
                metrics[$"{locus}DonorIdsSent"] = donorIdsSent[i];
                metrics[$"{locus}DonorsOut"] = donorsOut[i];
                metrics[$"{locus}PGroupIdMs"] = Ms(pGroupIdTicks[i]);
                metrics[$"{locus}PGroupsPosition1"] = pGroupsPosition1[i];
                metrics[$"{locus}PGroupsPosition2"] = pGroupsPosition2[i];
                metrics[$"{locus}UntypedMs"] = Ms(untypedTicks[i]);
                metrics[$"{locus}UntypedDonors"] = untypedDonors[i];
                metrics[$"{locus}MaterialisedDonors"] = materialisedDonors[i];
            }

            var props = new Dictionary<string, string>(extraProperties ?? new Dictionary<string, string>())
            {
                ["Run"] = runNumber.ToString(),
                ["SubSearch"] = subSearchIndex.ToString(),
                ["SubSearchCount"] = subSearchCount.ToString(),
                ["LociOrder"] = string.Join(",", lociOrder.Distinct()),
            };

            logger.SendEvent("Matching timings", LogLevel.Info, props, metrics);
        }
    }
}
