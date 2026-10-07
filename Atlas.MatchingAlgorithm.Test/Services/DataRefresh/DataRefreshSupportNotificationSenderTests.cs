using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.SupportMessages;
using Atlas.Common.Notifications;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using AutoFixture;
using NSubstitute;
using NUnit.Framework;
using BatchStatus = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationBatchStatus;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh;

[TestFixture]
public class DataRefreshSupportNotificationSenderTests
{
    private Fixture fixture;
    private INotificationSender notificationSender;

    private IDataRefreshSupportNotificationSender sender;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        notificationSender = Substitute.For<INotificationSender>();

        sender = new DataRefreshSupportNotificationSender(notificationSender);
    }

    [Test]
    public async Task SendPrecomputationStallAlert_SendsAHighPriorityAlertThatNamesTheRunAndCountsTheBatches()
    {
        var recordId = fixture.Create<int>();
        var runId = fixture.Create<int>();
        var requestedBatchCount = fixture.Create<int>();
        var batchCounts = new DonorGenotypePrecomputationBatchCounts(
            new Dictionary<BatchStatus, int> { [BatchStatus.Requested] = requestedBatchCount },
            0);

        await sender.SendPrecomputationStallAlert(recordId, runId, TimeSpan.FromMinutes(fixture.Create<int>()), batchCounts);

        await notificationSender.Received(1).SendAlert(
            Arg.Is<string>(summary => summary.Contains($"#{recordId}")),
            Arg.Is<string>(description => description.Contains($"run {runId}") && description.Contains($"Requested {requestedBatchCount}")),
            Priority.High,
            Arg.Any<string>());
    }

    [TestCase(1, 1000, 0.001, TestName = "At the threshold")]
    [TestCase(1, 2000, 0.001, TestName = "Below the threshold")]
    public async Task SendPrecomputationFailureSummary_AtOrBelowTheThreshold_SendsAMediumPriorityAlert(
        int failedDonorCount,
        int totalDonorCount,
        double maxFailedDonorFraction)
    {
        var summary = FailureSummary(failedDonorCount, totalDonorCount);

        await sender.SendPrecomputationFailureSummary(fixture.Create<int>(), fixture.Create<int>(), summary, maxFailedDonorFraction);

        await notificationSender.Received(1).SendAlert(
            Arg.Any<string>(),
            Arg.Is<string>(description => description.Contains("at or below the threshold") && description.Contains("The data refresh continued.")),
            Priority.Medium,
            Arg.Any<string>());
    }

    [TestCase(2, 1000, 0.001, TestName = "Above the threshold")]
    [TestCase(1, 1000, 0, TestName = "One failed donor with a threshold of 0")]
    public async Task SendPrecomputationFailureSummary_AboveTheThreshold_SendsAHighPriorityAlert(
        int failedDonorCount,
        int totalDonorCount,
        double maxFailedDonorFraction)
    {
        var summary = FailureSummary(failedDonorCount, totalDonorCount);

        await sender.SendPrecomputationFailureSummary(fixture.Create<int>(), fixture.Create<int>(), summary, maxFailedDonorFraction);

        await notificationSender.Received(1).SendAlert(
            Arg.Any<string>(),
            Arg.Is<string>(description => description.Contains("above the threshold") && description.Contains("The data refresh continued.")),
            Priority.High,
            Arg.Any<string>());
    }

    [Test]
    public async Task SendPrecomputationFailureSummary_NamesTheRecordAndTheRun_AndListsEverySample()
    {
        var recordId = fixture.Create<int>();
        var runId = fixture.Create<int>();
        var summary = FailureSummary(fixture.Create<int>(), fixture.Create<int>());

        await sender.SendPrecomputationFailureSummary(recordId, runId, summary, fixture.Create<double>());

        await notificationSender.Received(1).SendAlert(
            Arg.Is<string>(alertSummary => alertSummary.Contains($"#{recordId}")),
            Arg.Is<string>(description =>
                description.Contains($"run {runId}") && summary.Samples.All(sample => description.Contains(sample.FailureMessage))),
            Arg.Any<Priority>(),
            Arg.Any<string>());
    }

    private DonorGenotypePrecomputationFailureSummary FailureSummary(int failedDonorCount, int totalDonorCount) =>
        new(
            fixture.Create<int>(),
            fixture.Create<int>(),
            failedDonorCount,
            totalDonorCount,
            fixture.CreateMany<DonorGenotypePrecomputationFailureSample>().ToList());
}
