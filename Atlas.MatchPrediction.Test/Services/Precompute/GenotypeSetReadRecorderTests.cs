using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Services.Precompute;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.Precompute;

[TestFixture]
internal class GenotypeSetReadRecorderTests
{
    private const string Container = "records";
    private const int DataRefreshRecordId = 42;
    private static readonly DateTimeOffset RecordedAt = new(2026, 11, 19, 10, 30, 15, 123, TimeSpan.Zero);

    private static readonly IReadOnlyCollection<DonorInput> Donors =
    [
        new() { DonorIds = [1, 2] },
        new() { DonorIds = [3] },
    ];

    private GenotypeSetReadRecordingSettings settings;
    private IGenotypeSetReadRecordWriter writer;
    private IAtlasLogger logger;
    private GenotypeSetReadRecorder recorder;

    [SetUp]
    public void SetUp()
    {
        settings = new GenotypeSetReadRecordingSettings { Enabled = true, BlobContainer = Container };
        writer = Substitute.For<IGenotypeSetReadRecordWriter>();
        logger = Substitute.For<IAtlasLogger>();
        recorder = new GenotypeSetReadRecorder(settings, writer, logger);
    }

    [Test]
    public void Settings_AreOffByDefault()
    {
        new GenotypeSetReadRecordingSettings().Enabled.Should().BeFalse();
    }

    [Test]
    public async Task Record_WhenOff_WritesNothing()
    {
        settings.Enabled = false;

        recorder.Record(Request(), Donors, StoredContext());

        // Writing runs on the thread pool, so give a write that should not happen time to show up.
        await Task.Delay(200);
        await writer.DidNotReceiveWithAnyArgs().Write(default, default, default);
    }

    [Test]
    public async Task Record_WhenOn_WritesTheBatchToTheContainer()
    {
        var written = new TaskCompletionSource<GenotypeSetReadRecord>(TaskCreationOptions.RunContinuationsAsynchronously);
        writer.Write(Container, Arg.Any<string>(), Arg.Do<GenotypeSetReadRecord>(r => written.TrySetResult(r))).Returns(Task.CompletedTask);

        recorder.Record(Request(), Donors, StoredContext());

        var record = await written.Task.WaitAsync(TimeSpan.FromSeconds(10));
        record.SearchRequestId.Should().Be("search-id");
        record.DonorInputs.Should().BeEquivalentTo(new[] { new[] { 1, 2 }, new[] { 3 } });
    }

    [Test]
    public void Record_WhenTheRecordCannotBeBuilt_DoesNotThrowAndLogs()
    {
        var act = () => recorder.Record(Request(), null, StoredContext());

        act.Should().NotThrow();
        logger.ReceivedWithAnyArgs(1).SendException(default, default, default);
    }

    [Test]
    public async Task Write_WhenTheUploadFails_DoesNotThrowAndLogs()
    {
        writer.Write(default, default, default).ThrowsAsyncForAnyArgs(new Exception("storage is down"));
        var record = GenotypeSetReadRecorder.BuildRecord(Request(), Donors, StoredContext(), RecordedAt);

        var act = () => recorder.Write(record);

        await act.Should().NotThrowAsync();
        logger.Received(1).SendException(
            Arg.Any<Exception>(),
            LogLevel.Warn,
            Arg.Is<Dictionary<string, string>>(p =>
                p["EventName"] == GenotypeSetReadRecorder.RecordFailedEventName && p["SearchRequestId"] == "search-id"));
    }

    [Test]
    public void BuildRecord_OnTheStoredPath_HasTheKeyPartsTheOfflineScriptNeeds()
    {
        var request = Request();
        request.ExcludedLoci = [Locus.C];

        var record = GenotypeSetReadRecorder.BuildRecord(request, Donors, StoredContext(), RecordedAt);

        record.Should().BeEquivalentTo(new GenotypeSetReadRecord(
            RecordedAt,
            "search-id",
            DataRefreshRecordId,
            "A,B,Dqb1,Drb1",
            "ABDrb1Dqb1",
            UsePrecomputedGenotypeSets: true,
            BatchFallbackReason: null,
            [[1, 2], [3]]));
    }

    [Test]
    public void BuildRecord_OnTheLivePath_StillHasTheDonorInputsAndLoci()
    {
        var context = DonorGenotypeSetBatchContext.Disabled(DataRefreshRecordId, null);

        var record = GenotypeSetReadRecorder.BuildRecord(Request(), Donors, context, RecordedAt);

        record.UsePrecomputedGenotypeSets.Should().BeFalse();
        record.BatchFallbackReason.Should().Be(nameof(DonorGenotypeSetSource.PrecomputeDisabled));
        record.AllowedLociKey.Should().BeNull();
        record.AllowedLoci.Should().Be("A,B,C,Dqb1,Drb1");
        record.MatchingAlgorithmDataRefreshRecordId.Should().Be(DataRefreshRecordId);
        record.DonorInputs.Should().BeEquivalentTo(new[] { new[] { 1, 2 }, new[] { 3 } });
    }

    [Test]
    public void BlobName_StartsWithTheRefreshRecordThenTheDateThenTheSearch()
    {
        var record = GenotypeSetReadRecorder.BuildRecord(Request(), Donors, StoredContext(), RecordedAt);

        GenotypeSetReadRecorder.BlobName(record).Should().MatchRegex(@"^42/2026-11-19/search-id/103015123-[0-9a-f]{32}\.json$");
    }

    [Test]
    public void BlobName_WhenTheRefreshRecordIsUnknown_UsesTheUnknownFolder()
    {
        var request = Request();
        request.MatchingAlgorithmDataRefreshRecordId = null;
        var record = GenotypeSetReadRecorder.BuildRecord(request, Donors, StoredContext(), RecordedAt);

        GenotypeSetReadRecorder.BlobName(record).Should().StartWith($"{GenotypeSetReadRecorder.UnknownRefreshRecordFolder}/");
    }

    [Test]
    public void BlobName_IsUniquePerCall()
    {
        var record = GenotypeSetReadRecorder.BuildRecord(Request(), Donors, StoredContext(), RecordedAt);

        GenotypeSetReadRecorder.BlobName(record).Should().NotBe(GenotypeSetReadRecorder.BlobName(record));
    }

    private static IdentifiedMatchProbabilityRequest Request() => new()
    {
        SearchRequestId = "search-id",
        MatchingAlgorithmDataRefreshRecordId = DataRefreshRecordId,
        UsePrecomputedGenotypeSets = true,
    };

    private static DonorGenotypeSetBatchContext StoredContext() =>
        DonorGenotypeSetBatchContext.Enabled(
            DataRefreshRecordId,
            new PrecomputedDonorGenotypeSetLookup("ABDrb1Dqb1", null, new Dictionary<int, PrecomputedDonorGenotypeSetRow>()),
            null);
}
