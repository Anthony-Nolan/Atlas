using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

/// <summary>Starts the content hash backfill in the background of the process.</summary>
public interface IFrequencySetContentHashBackfillStarter
{
    /// <summary>Starts a run of the backfill in the background, unless a run goes on in this process: then the start joins it.</summary>
    void Start();

    /// <summary>Whether a run of the backfill goes on in this process, or is about to start.</summary>
    bool IsRunning { get; }
}

/// <summary>
/// Runs <see cref="IFrequencySetContentHashBackfill"/> in the background of the app, when the backfill endpoint asks for
/// it. A run reads the frequencies of each set with no content hash: longer than an HTTP call can wait.
///
/// <para>
/// One run at a time in each process. A run stops between two sets when the app stops, and a new start continues it: it
/// hashes only the sets that still have no hash. Two processes can each run the backfill: they read the same sets, and the
/// write of a hash keeps the first one, so this costs reads and does no harm.
/// </para>
/// </summary>
/// <remarks>
/// Takes only the scope factory and its logger: the backfill and the repositories are scoped, and a hosted service is a
/// singleton.
/// </remarks>
internal sealed class FrequencySetContentHashBackfillWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<FrequencySetContentHashBackfillWorker> logger) : BackgroundService, IFrequencySetContentHashBackfillStarter
{
    private readonly Channel<bool> starts = Channel.CreateBounded<bool>(1);

    // 1 from a start until the end of its run.
    private int running;

    /// <inheritdoc />
    public bool IsRunning => Volatile.Read(ref running) == 1;

    /// <inheritdoc />
    public void Start()
    {
        if (Interlocked.CompareExchange(ref running, 1, 0) == 0)
        {
            starts.Writer.TryWrite(true);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var _ in starts.Reader.ReadAllAsync(stoppingToken))
            {
                await RunOnce(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The app stops.
        }
    }

    /// <summary>Never throws, so that a failed run does not stop the worker: the next start runs the backfill again.</summary>
    internal async Task RunOnce(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IFrequencySetContentHashBackfill>().Run(stoppingToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "The content hash backfill of the haplotype frequency sets failed. Call BackfillHaplotypeFrequencySetContentHashes again to continue it.");
        }
        finally
        {
            Volatile.Write(ref running, 0);
        }
    }
}
