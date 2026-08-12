using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.MultipleAlleleCodeDictionary.Services;

/// <summary>
/// The single, process-wide set of known MACs.
/// </summary>
/// <remarks>
/// MAC data is append-only and immutable once published, so it needs a plain lookup table rather than a general purpose
/// expiring cache: one dictionary, held for the lifetime of the process, instead of one cache entry (plus one lazy wrapper)
/// per MAC. At the ~567k MACs seen during a data refresh, the per-entry overhead of the latter ran to ~100-150MB.
/// </remarks>
internal interface IMacStore
{
    /// <summary>
    /// How many MACs are currently held. Note this is only the full set of MACs once <see cref="WarmOnce"/> has run.
    /// </summary>
    int Count { get; }

    bool TryGetMac(string macCode, out MacValue mac);

    void AddMac(string macCode, MacValue mac);

    /// <summary>
    /// Runs <paramref name="warmAction"/> at most once, however many times, and from however many threads, this is called.
    /// Concurrent callers all await the same warm. A warm that throws does not count, so it may be retried.
    /// </summary>
    /// <param name="warmAction">Expected to fill this store, via <see cref="AddMac"/>.</param>
    Task WarmOnce(Func<Task> warmAction);
}

/// <inheritdoc />
internal sealed class MacStore : IMacStore
{
    // Ordinal comparison: MAC codes are upper-case ASCII identifiers, so there is no reason to pay for culture-aware hashing.
    private readonly ConcurrentDictionary<string, MacValue> macs = new(StringComparer.Ordinal);

    private readonly SemaphoreSlim warmGate = new(1, 1);
    private volatile bool isWarm;

    public int Count => macs.Count;

    public bool TryGetMac(string macCode, out MacValue mac) => macs.TryGetValue(macCode, out mac);

    public void AddMac(string macCode, MacValue mac) => macs[macCode] = mac;

    public async Task WarmOnce(Func<Task> warmAction)
    {
        if (isWarm)
        {
            return;
        }

        await warmGate.WaitAsync();
        try
        {
            if (isWarm)
            {
                return;
            }

            await warmAction();
            isWarm = true;
        }
        finally
        {
            warmGate.Release();
        }
    }
}
