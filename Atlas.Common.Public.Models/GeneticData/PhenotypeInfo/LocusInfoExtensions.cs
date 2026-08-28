// ReSharper disable InconsistentNaming - want to use T/R to easily distinguish contained type and target type(s)
// ReSharper disable MemberCanBeInternal

namespace Atlas.Common.Public.Models.GeneticData.PhenotypeInfo
{
    /// <summary>
    /// The "is this position absent?" probes over a <see cref="LocusInfo{T}"/>.
    ///
    /// <para>
    /// ATL-233: these are extension methods, constrained to a reference <c>T</c>, and were instance
    /// methods on <see cref="LocusInfo{T}"/> up to and including 4.1.0. <see cref="LocusInfo{T}"/> does not constrain its
    /// <c>T</c>, so <c>Position1 == null</c> inside the class <b>compiled for a value type and was
    /// always false</b> - no error and no warning. <c>Position1And2Null()</c> would have quietly become
    /// always-false, and <c>Position1And2NotNull()</c> always-true, the first time anyone put a <c>struct</c> in a
    /// <see cref="LocusInfo{T}"/> and called them. A silent wrong answer, in code that decides whether a locus is
    /// typed at all.
    /// </para>
    ///
    /// <para>
    /// A constraint cannot be added to <see cref="LocusInfo{T}"/> itself - it legitimately carries value types
    /// (<c>LocusInfo&lt;bool&gt;</c>, <c>LocusInfo&lt;int?&gt;</c>, <c>LocusInfo&lt;PredictiveMatchCategory?&gt;</c>
    /// are all live) - so the probes move out to where the constraint can be stated. A value-type
    /// <c>T</c> now fails to compile at the call site, and the fix is to say what "absent" means for
    /// that type, using <see cref="LocusInfo{T}.BothPositions"/> or <see cref="LocusInfo{T}.EitherPosition"/>.
    /// </para>
    /// </summary>
    public static class LocusInfoExtensions
    {
        /// <returns>true if neither position holds a value.</returns>
        public static bool Position1And2Null<T>(this LocusInfo<T> locusInfo) where T : class?
        {
            return locusInfo.Position1 == null && locusInfo.Position2 == null;
        }

        /// <returns>true if both positions hold a value.</returns>
        public static bool Position1And2NotNull<T>(this LocusInfo<T> locusInfo) where T : class?
        {
            return locusInfo.Position1 != null && locusInfo.Position2 != null;
        }

        /// <returns>true if exactly one of the two positions holds a value.</returns>
        public static bool SinglePositionNull<T>(this LocusInfo<T> locusInfo) where T : class?
        {
            return locusInfo.Position1 == null ^ locusInfo.Position2 == null;
        }
    }
}
