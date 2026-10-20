using System;
using System.Threading;

namespace Atlas.MatchPrediction.Test.TestHelpers;

/// <summary>
/// A <see cref="TimeProvider"/> whose timers fire at once, so <c>Task.Delay(delay, timeProvider)</c> ends at once, whatever
/// the delay. Its clock is the system clock.
/// </summary>
/// <remarks>
/// For tests that run real code with a delay that is not what they test - e.g. the second write of the active haplotype
/// frequency sets, 30 seconds after an import.
/// </remarks>
public class ImmediateTimersTimeProvider : TimeProvider
{
    public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period) =>
        base.CreateTimer(callback, state, TimeSpan.Zero, period);
}
