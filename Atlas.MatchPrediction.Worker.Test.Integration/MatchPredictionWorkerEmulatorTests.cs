using System.Text;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.ServiceBus;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Worker.Services;
using Atlas.MatchPrediction.Worker.Settings;
using Atlas.SearchTracking.Common.Dispatchers;
using AwesomeAssertions;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Worker.Test.Integration;

/// <summary>
/// ATL-546: runs the real <see cref="MatchPredictionWorker"/> and <see cref="ParallelMatchPredictionBatchRunner"/>
/// against the Azure Service Bus emulator, so Service Bus itself retries the batch and counts the deliveries.
/// Only the batch work is faked: the blob download fails on the first deliveries, then works.
/// Its own build pipeline job starts the emulator and sets RUN_SERVICE_BUS_EMULATOR_TESTS; without that variable the
/// fixture ignores itself.
/// </summary>
[TestFixture]
internal class MatchPredictionWorkerEmulatorTests
{
    private const string EmulatorVariable = "RUN_SERVICE_BUS_EMULATOR_TESTS";
    private const string ConnectionString =
        "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
    private const string AdminConnectionString =
        "Endpoint=sb://localhost:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
    private const string RequestsSubscription = "match-prediction";
    private const string ResultsSubscription = "aggregator";

    // Lower than the deployed 10 to keep the test short. The worker setting and the subscription get the same value,
    // as terraform gives them.
    private const int MaxDeliveryCount = 3;

    // Long enough for every delivery of one batch on the emulator.
    private static readonly TimeSpan ResultWait = TimeSpan.FromMinutes(1);
    // The results are already on the subscription by then, so a short wait is enough to see that no more come.
    private static readonly TimeSpan NoMoreResultsWait = TimeSpan.FromSeconds(3);

    private ServiceBusAdministrationClient admin;
    private ServiceBusClient client;
    private string requestsTopic;
    private string resultsTopic;

    [SetUp]
    public async Task SetUp()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EmulatorVariable)))
        {
            Assert.Ignore($"Set {EmulatorVariable} and start the Service Bus emulator. See README_DevelopmentStartUpGuide.md.");
        }

        var suffix = Guid.NewGuid().ToString("N");
        requestsTopic = "mp-requests-" + suffix;
        resultsTopic = "mp-results-" + suffix;
        admin = new ServiceBusAdministrationClient(AdminConnectionString);

        try
        {
            await admin.CreateTopicAsync(requestsTopic);
            await admin.CreateSubscriptionAsync(
                new CreateSubscriptionOptions(requestsTopic, RequestsSubscription) { MaxDeliveryCount = MaxDeliveryCount });
            // Without sessions, unlike the deployed aggregator subscription: the test only counts the results.
            await admin.CreateTopicAsync(resultsTopic);
            await admin.CreateSubscriptionAsync(new CreateSubscriptionOptions(resultsTopic, ResultsSubscription));
        }
        catch
        {
            // TearDown does not run when SetUp fails.
            await DeleteTopics();
            throw;
        }

        client = new ServiceBusClient(ConnectionString);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (client != null)
        {
            await client.DisposeAsync();
            client = null;
            await DeleteTopics();
        }
    }

    /// <remarks>
    /// The worker reports a batch as failed only on its last delivery, so the search gets one notification:
    /// a success if any delivery worked, else one failure, and Service Bus dead-letters the request.
    /// Before ATL-546 every failed delivery published a failure result, and this test got several results.
    /// </remarks>
    [TestCase(1, TestName = "RunBatch_FailsOnFirstDelivery_PublishesOnlySuccessResult")]
    [TestCase(MaxDeliveryCount - 1, TestName = "RunBatch_SucceedsOnLastDelivery_PublishesOnlySuccessResult")]
    [TestCase(MaxDeliveryCount, TestName = "RunBatch_FailsOnEveryDelivery_PublishesOneFailureResultAndDeadLettersRequest")]
    public async Task RunBatch_PublishesOneResultForBatchOutcome(int failingDeliveries)
    {
        var batchSucceeds = failingDeliveries < MaxDeliveryCount;
        var expectedDeliveries = batchSucceeds ? failingDeliveries + 1 : MaxDeliveryCount;
        var request = BuildRequest();

        // Take: a blob download that fails on the first deliveries.
        var downloads = 0;
        var blobDownloader = Substitute.For<IBlobDownloader>();
        blobDownloader.Download<MultipleDonorMatchProbabilityInput>(Arg.Any<string>(), Arg.Any<string>())
            .Returns(_ => Interlocked.Increment(ref downloads) <= failingDeliveries
                ? throw new InvalidOperationException("Batch failed on purpose by the test.")
                : Task.FromResult(new MultipleDonorMatchProbabilityInput()));

        // Send: the batch request, and let the worker process it until Service Bus stops delivering it.
        await using (var sender = client.CreateSender(requestsTopic))
        {
            await sender.SendMessageAsync(new ServiceBusMessage(JsonConvert.SerializeObject(request)));
        }

        await using var resultsReceiver = client.CreateReceiver(resultsTopic, ResultsSubscription);
        await using var deadLetterReceiver = client.CreateReceiver(
            requestsTopic, RequestsSubscription, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
        var results = new List<ParallelMatchPredictionBatchResult>();
        ServiceBusReceivedMessage deadLetteredRequest;

        var worker = BuildWorker(blobDownloader, out var processor);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            // Why: the batch outcome is the last thing the worker publishes, so wait for it. Before ATL-546 a failure
            // came first; it is collected too and fails the count below.
            results.AddRange(await ReceiveResultsUntil(resultsReceiver, r => r.IsSuccessful == batchSucceeds, ResultWait));

            // Why: a failed batch is dead-lettered after its last delivery, so this is when its deliveries are over.
            deadLetteredRequest = batchSucceeds ? null : await deadLetterReceiver.ReceiveMessageAsync(ResultWait);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
            await processor.DisposeAsync();
        }

        // Every delivery has been processed now, so any extra result would already be on the subscription.
        results.AddRange(await ReceiveResultsUntil(resultsReceiver, _ => false, NoMoreResultsWait));

        downloads.Should().Be(expectedDeliveries, "Service Bus should deliver the batch until it works or no retries are left");
        results.Should().ContainSingle("the search must get one notification for the batch")
            .Which.Should().Match<ParallelMatchPredictionBatchResult>(r =>
                r.IsSuccessful == batchSucceeds && r.BatchId == request.BatchId && r.ParallelRunId == request.ParallelRunId);

        if (batchSucceeds)
        {
            // The success is published before the request is completed, and only a failed last delivery dead-letters it.
            (await deadLetterReceiver.PeekMessageAsync()).Should().BeNull("a batch that worked must not be dead-lettered");
        }
        else
        {
            deadLetteredRequest.Should().NotBeNull("a batch that failed on every delivery should be dead-lettered for replay");
            // Not DeliveryCount: receiving from the dead-letter queue is one more delivery, so it is MaxDeliveryCount + 1.
            deadLetteredRequest.DeadLetterReason.Should().Be("MaxDeliveryCountExceeded");
            JsonConvert.DeserializeObject<ParallelMatchPredictionBatchRequest>(deadLetteredRequest.Body.ToString())
                .BatchId.Should().Be(request.BatchId);
        }
    }

    /// <summary>
    /// The worker as Startup builds it, with the real processor options and result publisher; only the batch work is faked.
    /// </summary>
    private MatchPredictionWorker BuildWorker(IBlobDownloader blobDownloader, out ServiceBusProcessor processor)
    {
        var algorithm = Substitute.For<IParallelMatchPredictionAlgorithm>();
        algorithm.RunBatch(Arg.Any<MultipleDonorMatchProbabilityInput>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new ParallelMatchPredictionBatchOutput("batch-result.json", 1, new Dictionary<int, int>()));

        var workerSettings = new MatchPredictionWorkerSettings
        {
            RequestsSubscription = RequestsSubscription,
            RequestsMaxDeliveryCount = MaxDeliveryCount,
            MaxConcurrentCalls = 1,
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(blobDownloader);
        services.AddSingleton(algorithm);
        services.AddSingleton(Substitute.For<IMatchPredictionSearchTrackingDispatcher>());
        services.AddSingleton<ISessionMessagePublisher<ParallelMatchPredictionBatchResult>>(sp =>
            new SessionMessagePublisher<ParallelMatchPredictionBatchResult>(
                new EmulatorTopicClientFactory(client),
                resultsTopic,
                sendRetryCount: 1,
                sendRetryCooldownSeconds: 1,
                sp.GetRequiredService<ILogger<SessionMessagePublisher<ParallelMatchPredictionBatchResult>>>()));
        services.AddSingleton(Options.Create(new AzureStorageSettings { MatchPredictionRequestsBlobContainer = "requests" }));
        services.AddSingleton(Options.Create(new MatchPredictionRequestsSettings { MaxParallelism = 1 }));
        services.AddSingleton(Options.Create(workerSettings));
        services.AddScoped<IParallelMatchPredictionBatchRunner, ParallelMatchPredictionBatchRunner>();
        var serviceProvider = services.BuildServiceProvider();

        processor = client.CreateProcessor(requestsTopic, RequestsSubscription, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = workerSettings.MaxConcurrentCalls,
            PrefetchCount = workerSettings.PrefetchCount,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(workerSettings.MaxAutoLockRenewalMinutes),
        });

        return new MatchPredictionWorker(
            processor,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<MatchPredictionWorker>.Instance);
    }

    /// <summary>
    /// Receives and completes results until one matches <paramref name="isLast"/> or <paramref name="wait"/> runs out.
    /// </summary>
    /// <returns>Every result received, including the one that matched.</returns>
    private static async Task<List<ParallelMatchPredictionBatchResult>> ReceiveResultsUntil(
        ServiceBusReceiver receiver,
        Func<ParallelMatchPredictionBatchResult, bool> isLast,
        TimeSpan wait)
    {
        var results = new List<ParallelMatchPredictionBatchResult>();
        var deadline = DateTime.UtcNow + wait;

        while (DateTime.UtcNow < deadline)
        {
            var message = await receiver.ReceiveMessageAsync(deadline - DateTime.UtcNow);
            if (message == null)
            {
                break;
            }

            await receiver.CompleteMessageAsync(message);
            var result = JsonConvert.DeserializeObject<ParallelMatchPredictionBatchResult>(Encoding.UTF8.GetString(message.Body));
            results.Add(result);

            if (isLast(result))
            {
                break;
            }
        }

        return results;
    }

    private static ParallelMatchPredictionBatchRequest BuildRequest() => new()
    {
        BlobLocation = "batch-request.json",
        SearchRequestId = Guid.NewGuid().ToString(),
        IsRepeatSearch = false,
        ParallelRunId = 1,
        BatchId = 2,
        BatchSequenceNumber = 0,
    };

    private async Task DeleteTopics()
    {
        foreach (var topic in new[] { requestsTopic, resultsTopic })
        {
            if (await admin.TopicExistsAsync(topic))
            {
                await admin.DeleteTopicAsync(topic);
            }
        }
    }

    private class EmulatorTopicClientFactory(ServiceBusClient client) : ITopicClientFactory
    {
        public ITopicClient BuildTopicClient(string topicName) => new TopicClient(client.CreateSender(topicName));
    }
}
