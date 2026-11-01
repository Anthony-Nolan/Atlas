using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring
{
    /// <summary>The steps of scoring that <see cref="ScoringStageTimings"/> times (ATL-446).</summary>
    /// <remarks>The names are logged as metric names (<c>Stage{Name}Ms</c>), so renaming one changes what App Insights queries see.</remarks>
    public enum ScoringStage
    {
        /// <summary>HLA Metadata Dictionary scoring lookups for the patient typing, once per request.</summary>
        PatientMetadataLookup,

        /// <summary>HLA Metadata Dictionary scoring lookups for one donor's typing (all positions run concurrently, so this is wall-clock time).</summary>
        DonorMetadataLookup,

        /// <summary>Grading in both orientations, including the scoring cache key build and lookup.</summary>
        Grading,

        /// <summary>The grade calculations that missed the scoring cache. Part of <see cref="Grading"/>.</summary>
        GradeCalculation,

        /// <summary>Confidence, including the scoring cache key build and lookup.</summary>
        Confidence,

        /// <summary>The confidence calculations that missed the scoring cache. Part of <see cref="Confidence"/>.</summary>
        ConfidenceCalculation,

        /// <summary>Antigen matching, including the scoring cache key build and lookup.</summary>
        AntigenMatching,

        /// <summary>The antigen match calculations that missed the scoring cache. Part of <see cref="AntigenMatching"/>.</summary>
        AntigenMatchCalculation,

        /// <summary>The DPB1 TCE group match type: two TCE group lookups for the patient and two for the donor.</summary>
        Dpb1TceGroup,

        /// <summary>Building the score result: score details per locus and the aggregate score.</summary>
        BuildResult,

        /// <summary>Ranking the scored results.</summary>
        Ranking,

        /// <summary>Waiting for the next match from the matching stream (this is matching time, not scoring time).</summary>
        MatchStreamWait,

        /// <summary>The <c>CountAsync</c> over the match stream, which runs all of matching a first time (ATL-502).</summary>
        DuplicateMatchCount,

        /// <summary>Search tracking events sent from inside the scoring loop.</summary>
        SearchTrackingPublish,

        /// <summary>Batch scoring only: mapping the distinct phenotype results back to each donor id.</summary>
        ResultMapping,
    }

    /// <summary>
    /// Where one scoring request spent its time, by <see cref="ScoringStage"/>, with counts of HLA Metadata Dictionary
    /// lookups, scoring cache misses and donor typing ambiguity (ATL-446: find the scoring bottlenecks).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Collected through an <see cref="AsyncLocal{T}"/> rather than passed down, because the steps sit several calls deep
    /// in services shared by search scoring, batch scoring and single scoring requests. <see cref="Begin"/> starts a
    /// collection for the current async flow; <see cref="Time"/> and the <c>Record*</c> methods add to it, and do nothing
    /// (at the cost of a timestamp) when no collection is running.
    /// </para>
    /// <para>A step's time is wall-clock time, so it includes any wait for a thread, a lock or the garbage collector.</para>
    /// </remarks>
    public sealed class ScoringStageTimings
    {
        /// <summary>A lookup that takes longer than this is counted as a miss of the HLA Metadata Dictionary's in-memory cache.</summary>
        public static readonly TimeSpan SlowLookupThreshold = TimeSpan.FromMilliseconds(1);

        private static readonly int StageCount = Enum.GetValues<ScoringStage>().Length;
        private static readonly int LocusCount = Enum.GetValues<Locus>().Length;
        private static readonly int KindCount = Enum.GetValues<DonorTypingKind>().Length;
        private static readonly AsyncLocal<ScoringStageTimings> Current = new();

        private readonly long startTimestamp = Stopwatch.GetTimestamp();
        private readonly long[] ticks = new long[StageCount];
        private readonly long[] lookupsPerLocus = new long[LocusCount];
        private readonly long[] slowLookupsPerLocus = new long[LocusCount];
        private readonly long[] lookupTicksPerLocus = new long[LocusCount];
        private readonly long[] positionsPerKind = new long[KindCount];
        private readonly long[] donorsPerKind = new long[KindCount];
        private readonly long[] donorTicksPerKind = new long[KindCount];
        private long multipleAlleleAlleles;
        private long gradeCalculations;
        private long confidenceCalculations;
        private long antigenMatchCalculations;
        private long maxDonorTicks;

        /// <summary>
        /// The kind of HLA typing at a position, from the HLA Metadata Dictionary's scoring info, in order of ambiguity.
        /// A donor's kind is the most ambiguous kind at any of its scored positions.
        /// </summary>
        public enum DonorTypingKind
        {
            SingleAllele,
            ConsolidatedMolecular,
            MultipleAllele,
            Serology,
            NewAllele,
            Other,
        }

        public TimeSpan Elapsed => Stopwatch.GetElapsedTime(startTimestamp);
        public TimeSpan Get(ScoringStage stage) => TimeSpan.FromTicks(Interlocked.Read(ref ticks[(int)stage]));
        public long GradeCalculations => Interlocked.Read(ref gradeCalculations);
        public long ConfidenceCalculations => Interlocked.Read(ref confidenceCalculations);
        public long AntigenMatchCalculations => Interlocked.Read(ref antigenMatchCalculations);
        public long Donors => donorsPerKind.Sum();
        public long Lookups(Locus locus) => Interlocked.Read(ref lookupsPerLocus[(int)locus]);
        public long SlowLookups(Locus locus) => Interlocked.Read(ref slowLookupsPerLocus[(int)locus]);
        public long DonorsOfKind(DonorTypingKind kind) => Interlocked.Read(ref donorsPerKind[(int)kind]);

        /// <summary>
        /// Starts collecting for the current async flow; the returned value stops collecting when disposed. Call from the
        /// method that awaits the scoring, so the collection flows into every call it makes.
        /// </summary>
        public static Collection Begin()
        {
            var previous = Current.Value;
            var timings = new ScoringStageTimings();
            Current.Value = timings;
            return new Collection(timings, previous);
        }

        /// <summary>Times <paramref name="stage"/> until the returned value is disposed, if a collection is running.</summary>
        public static StageTimer Time(ScoringStage stage) => new(Current.Value, stage);

        /// <summary>Records one HLA Metadata Dictionary scoring lookup and how long it took.</summary>
        public static void RecordLookup(Locus locus, TimeSpan elapsed)
        {
            var timings = Current.Value;
            if (timings == null)
            {
                return;
            }

            Interlocked.Increment(ref timings.lookupsPerLocus[(int)locus]);
            Interlocked.Add(ref timings.lookupTicksPerLocus[(int)locus], elapsed.Ticks);
            if (elapsed >= SlowLookupThreshold)
            {
                Interlocked.Increment(ref timings.slowLookupsPerLocus[(int)locus]);
            }
        }

        public static void CountGradeCalculation() => Count(t => ref t.gradeCalculations);
        public static void CountConfidenceCalculation() => Count(t => ref t.confidenceCalculations);
        public static void CountAntigenMatchCalculation() => Count(t => ref t.antigenMatchCalculations);

        /// <summary>Records one scored donor: its typing kinds, by position, and its total scoring time.</summary>
        public static void RecordDonor(PhenotypeInfo<IHlaScoringMetadata> donorMetadata, TimeSpan elapsed)
        {
            var timings = Current.Value;
            if (timings == null)
            {
                return;
            }

            var donorKind = DonorTypingKind.SingleAllele;
            donorMetadata.EachPosition((_, _, metadata) =>
            {
                if (metadata == null)
                {
                    return;
                }

                var kind = KindOf(metadata.HlaScoringInfo);
                Interlocked.Increment(ref timings.positionsPerKind[(int)kind]);
                if (metadata.HlaScoringInfo is MultipleAlleleScoringInfo multiple)
                {
                    Interlocked.Add(ref timings.multipleAlleleAlleles, multiple.AlleleScoringInfos.Count());
                }

                if (kind > donorKind)
                {
                    donorKind = kind;
                }
            });

            Interlocked.Increment(ref timings.donorsPerKind[(int)donorKind]);
            Interlocked.Add(ref timings.donorTicksPerKind[(int)donorKind], elapsed.Ticks);

            long max;
            while (elapsed.Ticks > (max = Interlocked.Read(ref timings.maxDonorTicks))
                   && Interlocked.CompareExchange(ref timings.maxDonorTicks, elapsed.Ticks, max) != max)
            {
            }
        }

        /// <summary>
        /// Sends the unsampled "Scoring timings" event. The matching Functions app excludes events from adaptive sampling
        /// (host.json), so every event is kept.
        /// </summary>
        public void Send(IAtlasLogger logger, string path, int distinctDonorPhenotypes, IEnumerable<Locus> lociToScore)
        {
            static double Ms(long t) => TimeSpan.FromTicks(t).TotalMilliseconds;

            var metrics = new Dictionary<string, double>
            {
                ["TotalMs"] = Elapsed.TotalMilliseconds,
                ["Donors"] = Donors,
                ["DistinctDonorPhenotypes"] = distinctDonorPhenotypes,
                ["GradeCalculations"] = GradeCalculations,
                ["ConfidenceCalculations"] = ConfidenceCalculations,
                ["AntigenMatchCalculations"] = AntigenMatchCalculations,
                ["MultipleAlleleAlleles"] = Interlocked.Read(ref multipleAlleleAlleles),
                ["MaxDonorMs"] = Ms(Interlocked.Read(ref maxDonorTicks)),
            };

            foreach (var stage in Enum.GetValues<ScoringStage>())
            {
                metrics[$"Stage{stage}Ms"] = Get(stage).TotalMilliseconds;
            }

            foreach (var locus in Enum.GetValues<Locus>())
            {
                metrics[$"Lookups{locus}"] = Lookups(locus);
                metrics[$"SlowLookups{locus}"] = SlowLookups(locus);
                metrics[$"LookupMs{locus}"] = Ms(Interlocked.Read(ref lookupTicksPerLocus[(int)locus]));
            }

            foreach (var kind in Enum.GetValues<DonorTypingKind>())
            {
                metrics[$"Positions{kind}"] = Interlocked.Read(ref positionsPerKind[(int)kind]);
                metrics[$"Donors{kind}"] = DonorsOfKind(kind);
                metrics[$"DonorMs{kind}"] = Ms(Interlocked.Read(ref donorTicksPerKind[(int)kind]));
            }

            var props = new Dictionary<string, string>
            {
                ["Path"] = path,
                ["LociToScore"] = string.Join(",", lociToScore ?? Enumerable.Empty<Locus>()),
            };

            logger.SendEvent("Scoring timings", LogLevel.Info, props, metrics);
        }

        private static DonorTypingKind KindOf(IHlaScoringInfo info) => info switch
        {
            SingleAlleleScoringInfo => DonorTypingKind.SingleAllele,
            ConsolidatedMolecularScoringInfo => DonorTypingKind.ConsolidatedMolecular,
            MultipleAlleleScoringInfo => DonorTypingKind.MultipleAllele,
            SerologyScoringInfo => DonorTypingKind.Serology,
            NewAlleleScoringInfo => DonorTypingKind.NewAllele,
            _ => DonorTypingKind.Other,
        };

        private delegate ref long Field(ScoringStageTimings timings);

        private static void Count(Field field)
        {
            var timings = Current.Value;
            if (timings != null)
            {
                Interlocked.Increment(ref field(timings));
            }
        }

        private void Add(ScoringStage stage, long elapsedTicks) => Interlocked.Add(ref ticks[(int)stage], elapsedTicks);

        /// <summary>A running collection. Dispose to stop collecting; <see cref="Timings"/> stays readable.</summary>
        public readonly struct Collection : IDisposable
        {
            private readonly ScoringStageTimings previous;

            internal Collection(ScoringStageTimings timings, ScoringStageTimings previous)
            {
                Timings = timings;
                this.previous = previous;
            }

            public ScoringStageTimings Timings { get; }

            public void Dispose() => Current.Value = previous;
        }

        /// <summary>Times one stage. A no-op when no collection was running at its start.</summary>
        public readonly struct StageTimer : IDisposable
        {
            private readonly ScoringStageTimings timings;
            private readonly ScoringStage stage;
            private readonly long startTimestamp;

            internal StageTimer(ScoringStageTimings timings, ScoringStage stage)
            {
                this.timings = timings;
                this.stage = stage;
                startTimestamp = timings == null ? 0 : Stopwatch.GetTimestamp();
            }

            public void Dispose() => timings?.Add(stage, Stopwatch.GetElapsedTime(startTimestamp).Ticks);
        }
    }
}
