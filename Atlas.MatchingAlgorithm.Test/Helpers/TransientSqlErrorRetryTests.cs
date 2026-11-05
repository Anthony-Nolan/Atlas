using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Helpers;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Helpers;

/// <summary>
/// Covers the retry loop. A <c>SqlException</c> has no public constructor, so these tests pass their own "is transient"
/// check. The integration tests in DonorStorageTests cover a real dropped connection.
/// </summary>
[TestFixture]
public class TransientSqlErrorRetryTests
{
    private static readonly IReadOnlyList<TimeSpan> ThreeRetries = [TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero];

    private class TransientTestException : Exception;

    private static bool IsTransientTestException(Exception exception) => exception is TransientTestException;

    [Test]
    public async Task Run_WhenTheFirstAttemptSucceeds_RunsItOnceAndDoesNotRetry()
    {
        var attempts = new List<int>();
        var retries = 0;

        await TransientSqlErrorRetry.Run(
            attempt =>
            {
                attempts.Add(attempt);
                return Task.CompletedTask;
            },
            (_, _) => retries++,
            ThreeRetries,
            IsTransientTestException);

        attempts.Should().Equal(1);
        retries.Should().Be(0);
    }

    [Test]
    public async Task Run_AfterTransientErrors_RetriesUntilAnAttemptSucceeds()
    {
        var attempts = new List<int>();
        var failedAttemptsSeenByOnRetry = new List<int>();

        await TransientSqlErrorRetry.Run(
            attempt =>
            {
                attempts.Add(attempt);
                return attempt < 3 ? throw new TransientTestException() : Task.CompletedTask;
            },
            (_, failedAttempt) => failedAttemptsSeenByOnRetry.Add(failedAttempt),
            ThreeRetries,
            IsTransientTestException);

        attempts.Should().Equal(1, 2, 3);
        failedAttemptsSeenByOnRetry.Should().Equal(1, 2);
    }

    [Test]
    public async Task Run_WhenEveryAttemptHasATransientError_ThrowsTheLastErrorAfterOneAttemptPerDelayPlusOne()
    {
        var attempts = 0;

        var act = () => TransientSqlErrorRetry.Run(
            _ =>
            {
                attempts++;
                throw new TransientTestException();
            },
            (_, _) => { },
            ThreeRetries,
            IsTransientTestException);

        await act.Should().ThrowAsync<TransientTestException>();
        attempts.Should().Be(4);
    }

    [Test]
    public async Task Run_WhenAnErrorIsNotTransient_ThrowsItWithoutARetry()
    {
        var attempts = 0;
        var retries = 0;

        var act = () => TransientSqlErrorRetry.Run(
            _ =>
            {
                attempts++;
                throw new InvalidOperationException();
            },
            (_, _) => retries++,
            ThreeRetries,
            IsTransientTestException);

        await act.Should().ThrowAsync<InvalidOperationException>();
        attempts.Should().Be(1);
        retries.Should().Be(0);
    }

    [Test]
    public async Task Run_WithNoDelays_DoesNotRetry()
    {
        var attempts = 0;

        var act = () => TransientSqlErrorRetry.Run(
            _ =>
            {
                attempts++;
                throw new TransientTestException();
            },
            (_, _) => { },
            [],
            IsTransientTestException);

        await act.Should().ThrowAsync<TransientTestException>();
        attempts.Should().Be(1);
    }

    [Test]
    public void IsTransient_ForANonSqlError_IsFalse()
    {
        TransientSqlErrorRetry.IsTransient(new InvalidOperationException("not a SQL error")).Should().BeFalse();
        TransientSqlErrorRetry.IsTransient(new AggregateException(new TimeoutException())).Should().BeFalse();
        TransientSqlErrorRetry.IsTransient(null).Should().BeFalse();
    }
}
