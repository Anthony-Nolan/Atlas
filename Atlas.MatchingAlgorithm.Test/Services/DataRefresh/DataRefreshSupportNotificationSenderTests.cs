using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.SupportMessages;
using Atlas.Common.Notifications;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Models.DataRefresh;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
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

    [Test]
    public async Task SendPrecomputationPausedAlert_SendsAHighPriorityAlertThatNamesTheRecordTheRunTheEndpointAndTheDecisions()
    {
        var recordId = fixture.Create<int>();
        var report = PausedReport();

        await sender.SendPrecomputationPausedAlert(recordId, report, true);

        await notificationSender.Received(1).SendAlert(
            Arg.Is<string>(summary => summary.StartsWith("Data refresh paused") && summary.Contains($"#{recordId}")),
            Arg.Is<string>(description =>
                description.Contains($"run {report.RunId}")
                && description.Contains($"\"DataRefreshRecordId\": {recordId}")
                && description.Contains(ResolvePausedDataRefreshRequest.EndpointName)
                && Enum.GetNames<DataRefreshManualDecision>().All(decision => description.Contains($"- {decision}:"))),
            Priority.High,
            Arg.Any<string>());
    }

    [Test]
    public async Task SendPrecomputationPausedAlert_ListsEverySample()
    {
        var report = PausedReport();

        await sender.SendPrecomputationPausedAlert(fixture.Create<int>(), report, true);

        await notificationSender.Received(1).SendAlert(
            Arg.Any<string>(),
            Arg.Is<string>(description => report.FailureSummary.Samples.All(sample => description.Contains(sample.FailureMessage))),
            Arg.Any<Priority>(),
            Arg.Any<string>());
    }

    [Test]
    public async Task SendPrecomputationPausedAlert_WhenTheScaleDownStarted_SaysThatTheDatabaseIsScalingDown()
    {
        await sender.SendPrecomputationPausedAlert(fixture.Create<int>(), PausedReport(), true);

        await notificationSender.Received(1).SendAlert(
            Arg.Any<string>(),
            Arg.Is<string>(description => description.Contains("is scaling the dormant database down")),
            Arg.Any<Priority>(),
            Arg.Any<string>());
    }

    [Test]
    public async Task SendPrecomputationPausedAlert_WhenAzureRefusedTheScaleDown_SaysToScaleTheDatabaseDownByHand()
    {
        await sender.SendPrecomputationPausedAlert(fixture.Create<int>(), PausedReport(), false);

        await notificationSender.Received(1).SendAlert(
            Arg.Any<string>(),
            Arg.Is<string>(description => description.Contains("Scale it down by hand")),
            Arg.Any<Priority>(),
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

    private DonorGenotypePrecomputationFailureReport PausedReport() =>
        new(
            fixture.Create<int>(),
            FailureSummary(fixture.Create<int>(), fixture.Create<int>()),
            fixture.Create<double>(),
            fixture.Create<int>());
}
