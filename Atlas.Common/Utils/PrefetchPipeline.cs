using Atlas.Common.ApplicationInsights;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Atlas.Common.Utils
{
    /// <summary>
    /// Overlaps a read side that produces items with a processing side that consumes them, so that two stages which
    /// contend for nothing cost roughly <c>max(read, process)</c> rather than <c>read + process</c>.
    /// </summary>
    /// <remarks>
    /// Extracted rather than written twice: the data refresh runs this shape in two places (reading donors from the
    /// master store, and reading them back out of the transient database to expand their HLA), and the failure
    /// handling below is subtle enough that a correctness fix applied to one copy would quietly miss the other.
    /// </remarks>
    public static class PrefetchPipeline
    {
        /// <summary>
        /// Runs <paramref name="readAhead"/> concurrently with <paramref name="process"/>, connected by a bounded
        /// queue, and returns whatever the processing side returns.
        /// </summary>
        /// <param name="queueDepth">
        /// How many items the read side may run ahead of the processing side. The queue applies back-pressure once
        /// full, so this bounds how much produced-but-unconsumed work is held in memory. Note the real cost is
        /// <c>queueDepth + 2</c> items: the processing side holds the one it is working on, and the read side the one
        /// it is blocked handing over.
        /// </param>
        /// <param name="readAhead">
        /// Produces items into the writer. Must not complete the writer - this method does that, and completing it
        /// early would end the processing side's enumeration prematurely. The second argument reports how many
        /// produced items are currently queued, for read sides that want to log how far ahead they are running; it is
        /// deliberately a probe rather than the reader itself, since the read side must never consume.
        /// </param>
        /// <param name="process">Consumes items from the reader, typically via <c>ReadAllAsync</c>.</param>
        /// <param name="readSideDescription">Names the read side in the one log message this can emit, e.g. "Donor read".</param>
        /// <param name="cancellationToken">
        /// Cancelling this stops both sides. The processing side is handed it directly; the read side is handed a
        /// linked token, so that whichever side notices first, the other is torn down too.
        /// </param>
        public static async Task<TResult> Run<TItem, TResult>(
            int queueDepth,
            Func<ChannelWriter<TItem>, Func<int>, CancellationToken, Task> readAhead,
            Func<ChannelReader<TItem>, CancellationToken, Task<TResult>> process,
            IAtlasLogger logger,
            string readSideDescription,
            CancellationToken cancellationToken)
        {
            var items = Channel.CreateBounded<TItem>(
                new BoundedChannelOptions(queueDepth)
                {
                    SingleReader = true,
                    SingleWriter = true,
                    FullMode = BoundedChannelFullMode.Wait
                });

            // Linked so the caller's own cancellation reaches the read side promptly. This is not what guarantees
            // teardown - the finally below cancels unconditionally on every exit path - it only shortens the window in
            // which a read side blocked on a full queue keeps waiting after the caller's token has already gone.
            using var readCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Bounded channels always support Count, so this needs no capability check at the call site.
            var readTask = RunReadSide(
                readAhead, items.Writer, () => items.Reader.Count, logger, readSideDescription, readCancellation.Token);

            try
            {
                return await process(items.Reader, cancellationToken);
            }
            finally
            {
                // Cancel before awaiting: a read side blocked on a full queue has to be released before it can
                // terminate. Awaiting at all is what stops this method returning, by any path, while the read side is
                // still running - otherwise an abandoned read side would go on consuming whatever it holds open after
                // the caller that owns it has unwound.
                await readCancellation.CancelAsync();
                await AwaitReadSideQuietly(readTask, logger, readSideDescription);
            }
        }

        private static async Task RunReadSide<TItem>(
            Func<ChannelWriter<TItem>, Func<int>, CancellationToken, Task> readAhead,
            ChannelWriter<TItem> writer,
            Func<int> queueDepth,
            IAtlasLogger logger,
            string readSideDescription,
            CancellationToken cancellationToken)
        {
            try
            {
                await readAhead(writer, queueDepth, cancellationToken);
                writer.TryComplete();
            }
            catch (Exception e)
            {
                // Logged here rather than left to whoever observes the queue. If the processing side has already failed
                // on its own it never reads the completion, so this is otherwise the only record of why the read side
                // stopped. Warn rather than Error: when the processing side does observe this, its own caller reports
                // the stage-level Error, and one incident should not raise two. Warn still clears the configured Info
                // threshold, so the cause survives even when nothing else reports it. Cancellation is expected, not a
                // failure, so it is excluded.
                if (e is not OperationCanceledException)
                {
                    logger.SendTrace($"{readSideDescription} failed: {e}", LogLevel.Warn);
                }

                // How a read-side failure reaches the processing side. ReadAllAsync surfaces it unwrapped, unlike
                // ReadAsync, so it keeps its type: that is what lets the caller still tell cancellation from failure
                // now the exception crosses threads to get there.
                writer.TryComplete(e);
            }
        }

        private static async Task AwaitReadSideQuietly(Task readTask, IAtlasLogger logger, string readSideDescription)
        {
            try
            {
                await readTask;
            }
            catch (Exception e)
            {
                // Deliberately defensive, and not expected to fire: RunReadSide resolves every exception into the
                // queue, so the read task completes even when the read itself failed. This exists because the await is
                // reached from a finally, where an exception would displace the one already propagating out of Run and
                // so lose the real failure. It is not a second reporting path for read errors.
                logger.SendTrace($"{readSideDescription} task ended with an exception: {e}", LogLevel.Verbose);
            }
        }
    }
}
