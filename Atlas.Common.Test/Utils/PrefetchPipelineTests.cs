using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Atlas.Common.Test.Utils
{
    /// <summary>
    /// The failure paths are the reason this is shared code rather than duplicated at each call site, so they are what
    /// these cover: which side a failure surfaces on, whether it keeps its type, and at what severity it is reported.
    /// </summary>
    [TestFixture]
    public class PrefetchPipelineTests
    {
        private const int QueueDepth = 2;
        private const string ReadSideDescription = "Test read";

        private IAtlasLogger logger;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<IAtlasLogger>();
        }

        [Test]
        public async Task Run_ReturnsWhatTheProcessingSideReturned()
        {
            var result = await Run(
                readAhead: WriteItems(1, 2, 3),
                process: async (reader, token) =>
                {
                    var seen = new List<int>();
                    await foreach (var item in reader.ReadAllAsync(token))
                    {
                        seen.Add(item);
                    }

                    return seen;
                });

            result.Should().Equal(1, 2, 3);
        }

        [Test]
        public async Task Run_LetsTheReadSideRunAheadOfTheProcessingSide()
        {
            // The whole point. The first item's processing blocks until the read side has produced a second one, which
            // serial code could never do because it would not read again until processing returned.
            var readAhead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var produced = 0;

            await Run(
                readAhead: async (writer, _, token) =>
                {
                    foreach (var item in Enumerable.Range(0, 8))
                    {
                        if (Interlocked.Increment(ref produced) >= 2)
                        {
                            readAhead.TrySetResult();
                        }

                        await writer.WriteAsync(item, token);
                    }
                },
                process: async (reader, token) =>
                {
                    var isFirst = true;
                    await foreach (var _ in reader.ReadAllAsync(token))
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                            readAhead.Task.Wait(TimeSpan.FromSeconds(30))
                                .Should().BeTrue("the read side should produce ahead of the processing side");
                        }
                    }

                    return 0;
                });
        }

        [Test]
        public async Task Run_ReportsTheQueueDepthToTheReadSide()
        {
            // Read sides log this to tell "running ahead" from "processing side starved", so it has to reflect what is
            // actually queued rather than being a constant.
            var observedDepths = new List<int>();

            await Run(
                readAhead: async (writer, queueDepth, token) =>
                {
                    foreach (var item in Enumerable.Range(0, 4))
                    {
                        observedDepths.Add(queueDepth());
                        await writer.WriteAsync(item, token);
                    }
                },
                process: (reader, token) => DrainAsync(reader, token));

            // Nothing is consumed until the queue fills, so depth climbs from empty to capacity.
            observedDepths.Should().StartWith(new[] {0, 1});
            observedDepths.Should().OnlyContain(depth => depth <= QueueDepth);
        }

        [Test]
        public async Task Run_WhenReadSideFails_SurfacesThatFailureWithItsOwnType()
        {
            // Callers distinguish cancellation from failure by type, so the exception must arrive unwrapped rather
            // than boxed in a ChannelClosedException.
            await Invoking(() => Run(
                    readAhead: (_, _, _) => throw new TimeoutException("read failed"),
                    process: (reader, token) => DrainAsync(reader, token)))
                .Should().ThrowAsync<TimeoutException>();
        }

        [Test]
        public async Task Run_WhenReadSideFails_LogsTheCauseBelowError()
        {
            // The failure also propagates out of Run, where the caller reports the stage-level Error. Logging the
            // cause below that keeps one incident to one Error, while still recording where it started.
            await Invoking(() => Run(
                    readAhead: (_, _, _) => throw new TimeoutException("read failed"),
                    process: (reader, token) => DrainAsync(reader, token)))
                .Should().ThrowAsync<TimeoutException>();

            logger.Received(1).SendTrace(
                Arg.Is<string>(m => m.Contains(ReadSideDescription) && m.Contains("read failed")),
                LogLevel.Warn,
                Arg.Any<Dictionary<string, string>>());
            logger.DidNotReceive().SendTrace(
                Arg.Any<string>(),
                Arg.Is<LogLevel>(level => level >= LogLevel.Error),
                Arg.Any<Dictionary<string, string>>());
        }

        [Test]
        public async Task Run_WhenTheProcessingSideNeverSeesTheReadFailure_StillLogsIt()
        {
            // The processing side failed on its own, so it never read the queue's completion and the read side's own
            // later failure reaches nobody. This log is the only record of it, so it must not be suppressed. Warn
            // clears the deployed Info threshold, which is what makes it survive here.
            await Invoking(() => Run(
                    readAhead: async (_, _, token) =>
                    {
                        // Fails while unwinding, after Run has already cancelled it on the processing side's behalf -
                        // so this is a genuinely different failure from the one propagating.
                        try
                        {
                            await Task.Delay(Timeout.Infinite, token);
                        }
                        catch (OperationCanceledException)
                        {
                            throw new InvalidOperationException("read teardown failed");
                        }
                    },
                    process: (_, _) => throw new NotSupportedException("processing failed")))
                .Should().ThrowAsync<NotSupportedException>();

            logger.Received(1).SendTrace(
                Arg.Is<string>(m => m.Contains(ReadSideDescription) && m.Contains("read teardown failed")),
                LogLevel.Warn,
                Arg.Any<Dictionary<string, string>>());
        }

        [Test]
        public async Task Run_WhenCancelled_DoesNotLogTheReadSideAsAFailure()
        {
            // Losing a lease is expected, not a failure, and the read side is always cancelled on the way out.
            using var cancellation = new CancellationTokenSource();

            await Invoking(() => Run(
                    readAhead: WriteItems(1, 2, 3),
                    process: async (reader, token) =>
                    {
                        await cancellation.CancelAsync();
                        return await DrainAsync(reader, token);
                    },
                    cancellationToken: cancellation.Token))
                .Should().ThrowAsync<OperationCanceledException>();

            logger.DidNotReceiveWithAnyArgs().SendTrace(default, default, default);
        }

        [Test]
        public async Task Run_WhenProcessingSideFails_StopsTheReadSideAndDoesNotReturnWhileItRuns()
        {
            // Without the cancellation the read side blocks forever on a full queue nothing is draining; without the
            // await, Run returns while it is still producing against resources its caller has finished with.
            var produced = 0;
            var readSideFinished = false;

            await Invoking(() => Run(
                    readAhead: async (writer, _, token) =>
                    {
                        try
                        {
                            foreach (var item in Enumerable.Range(0, 1000))
                            {
                                Interlocked.Increment(ref produced);
                                await writer.WriteAsync(item, token);
                            }
                        }
                        finally
                        {
                            readSideFinished = true;
                        }
                    },
                    process: (_, _) => throw new NotSupportedException("processing failed")))
                .Should().ThrowAsync<NotSupportedException>();

            readSideFinished.Should().BeTrue("Run must not return while the read side is still running");
            produced.Should().BeLessThan(10, "production is bounded by the queue, not by the items available");
        }

        private Task<int> Run(
            Func<ChannelWriter<int>, Func<int>, CancellationToken, Task> readAhead,
            Func<ChannelReader<int>, CancellationToken, Task<int>> process,
            CancellationToken cancellationToken = default) =>
            PrefetchPipeline.Run(QueueDepth, readAhead, process, logger, ReadSideDescription, cancellationToken);

        private Task<List<T>> Run<T>(
            Func<ChannelWriter<int>, Func<int>, CancellationToken, Task> readAhead,
            Func<ChannelReader<int>, CancellationToken, Task<List<T>>> process) =>
            PrefetchPipeline.Run(QueueDepth, readAhead, process, logger, ReadSideDescription, CancellationToken.None);

        private static Func<ChannelWriter<int>, Func<int>, CancellationToken, Task> WriteItems(params int[] items) =>
            async (writer, _, token) =>
            {
                foreach (var item in items)
                {
                    await writer.WriteAsync(item, token);
                }
            };

        private static async Task<int> DrainAsync(ChannelReader<int> reader, CancellationToken cancellationToken)
        {
            var count = 0;
            await foreach (var _ in reader.ReadAllAsync(cancellationToken))
            {
                count++;
            }

            return count;
        }

        private static Func<Task> Invoking(Func<Task> action) => action;
    }
}
