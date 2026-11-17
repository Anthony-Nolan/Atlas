using System;

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval
{
    /// <summary>
    /// ATL-526 experiment: limits matching to one range of donor IDs, to measure whether the matching time of a "piece"
    /// scales with the size of its donor ID range (proposal "resumable donor ID range pieces"). Experiment only, not for release.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The range comes from the environment variable (app setting) <c>ATL526_DONOR_ID_RANGE</c>, so it can be changed on DEV
    /// without a deploy. Format: <c>min-max</c> (both inclusive, for example <c>1-5500000</c>), or <c>min-</c> for a range
    /// with no upper end (for example <c>38000000-</c>).
    /// </para>
    /// <para>
    /// The range is used only when the ATL-526 experiment mode (<c>ATL526_MATCHING_MODE</c>) is on (not empty and not
    /// <c>Normal</c>), so a forgotten setting can never cut real search results. A value that cannot be read is ignored.
    /// </para>
    /// </remarks>
    public static class DonorIdRangeExperiment
    {
        public const string RangeSettingName = "ATL526_DONOR_ID_RANGE";
        private const string ModeSettingName = "ATL526_MATCHING_MODE";

        /// <summary>Set by tests only. When set, it is used instead of both app settings.</summary>
        internal static string RangeOverride { get; set; }

        /// <summary>The range to use for this search, or null for all donors.</summary>
        public static DonorIdRange Current
        {
            get
            {
                if (RangeOverride != null)
                {
                    return Parse(RangeOverride);
                }

                var mode = Environment.GetEnvironmentVariable(ModeSettingName);
                var experimentIsOn = !string.IsNullOrWhiteSpace(mode) && !mode.Trim().Equals("Normal", StringComparison.OrdinalIgnoreCase);
                return experimentIsOn ? Parse(Environment.GetEnvironmentVariable(RangeSettingName)) : null;
            }
        }

        /// <summary>Reads <c>min-max</c> or <c>min-</c>. Returns null when the value is empty or cannot be read.</summary>
        internal static DonorIdRange Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var parts = value.Trim().Split('-');
            if (parts.Length != 2 || !int.TryParse(parts[0].Trim(), out var min) || min < 0)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(parts[1]))
            {
                return new DonorIdRange(min, null);
            }

            if (!int.TryParse(parts[1].Trim(), out var max) || max < min)
            {
                return null;
            }

            return new DonorIdRange(min, max);
        }
    }

    /// <summary>A range of donor IDs. Both ends are inclusive. <see cref="Max"/> null means no upper end.</summary>
    public sealed record DonorIdRange(int Min, int? Max)
    {
        /// <summary>
        /// The SQL condition on the given donor ID column. The values are parsed integers, written as literals so that
        /// SQL Server can estimate the range for each query.
        /// </summary>
        public string SqlCondition(string donorIdColumn) =>
            Max == null
                ? $"{donorIdColumn} >= {Min}"
                : $"{donorIdColumn} BETWEEN {Min} AND {Max.Value}";

        public override string ToString() => Max == null ? $"{Min}-" : $"{Min}-{Max.Value}";
    }
}
