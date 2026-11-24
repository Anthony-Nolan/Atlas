using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.Settings;

namespace Atlas.MatchPrediction.Services.Precompute;

/// <summary>
/// Records which donor inputs a search batch would read precomputed genotype sets for, on both the stored and the live
/// path (ATL-571). The record is the input of the cache hit-rate measurement in ATL-347.
/// </summary>
/// <remarks>
/// Only donor ids are recorded, not the value key: in this version the search does not build the key, and the live path
/// does not read the stored rows. An offline SQL script finds the value key and its size from the donor ids, in the
/// transient database of the recorded refresh record, before the next Data Refresh wipes it.
/// </remarks>
public interface IGenotypeSetReadRecorder
{
    /// <summary>
    /// Starts recording the batch and returns at once. Never throws, and a failure to record never fails the search.
    /// </summary>
    void Record(IdentifiedMatchProbabilityRequest request, IReadOnlyCollection<DonorInput> donors, DonorGenotypeSetBatchContext batchContext);
}

/// <summary>
/// One search batch, as recorded. Holds no patient information: no patient typing and no request JSON.
/// </summary>
/// <param name="RecordedAtUtc">When the batch was resolved: the time of the read.</param>
/// <param name="SearchRequestId">Lets the replay find searches that ran at the same time.</param>
/// <param name="MatchingAlgorithmDataRefreshRecordId">
/// The refresh record whose transient database matching used. Null for a request that does not say (see
/// <see cref="DonorGenotypeSetSource.ActiveDatabaseUnknown"/>); such a batch cannot be resolved offline.
/// </param>
/// <param name="AllowedLoci">The loci match prediction ran on, by name, comma separated, in <see cref="Locus"/> order.</param>
/// <param name="AllowedLociKey">The allowed-loci key the stored rows were read with; null when no read ran.</param>
/// <param name="UsePrecomputedGenotypeSets">True on the stored path, false on the live path.</param>
/// <param name="BatchFallbackReason">The batch-wide reason no stored row was used, by name; null when the rows were read.</param>
/// <param name="DonorInputs">The donor ids of each donor input. Donor ids that share a donor input share one value key.</param>
public sealed record GenotypeSetReadRecord(
    DateTimeOffset RecordedAtUtc,
    string SearchRequestId,
    int? MatchingAlgorithmDataRefreshRecordId,
    string AllowedLoci,
    string AllowedLociKey,
    bool UsePrecomputedGenotypeSets,
    string BatchFallbackReason,
    IReadOnlyCollection<IReadOnlyCollection<int>> DonorInputs);

/// <summary>Writes one record to blob storage.</summary>
internal interface IGenotypeSetReadRecordWriter
{
    Task Write(string container, string blobName, GenotypeSetReadRecord record);
}

internal class GenotypeSetReadRecordWriter : IGenotypeSetReadRecordWriter
{
    private readonly BlobUploader uploader;

    public GenotypeSetReadRecordWriter(AzureStorageSettings azureStorageSettings, IAtlasLogger logger)
    {
        uploader = new BlobUploader(azureStorageSettings.ConnectionString, logger);
    }

    public Task Write(string container, string blobName, GenotypeSetReadRecord record) => uploader.Upload(container, blobName, record);
}

internal class GenotypeSetReadRecorder : IGenotypeSetReadRecorder
{
    internal const string RecordFailedEventName = "Genotype set read recording failed";
    internal const string UnknownRefreshRecordFolder = "unknown";

    private readonly GenotypeSetReadRecordingSettings settings;
    private readonly IGenotypeSetReadRecordWriter writer;
    private readonly IAtlasLogger logger;

    public GenotypeSetReadRecorder(
        GenotypeSetReadRecordingSettings settings,
        IGenotypeSetReadRecordWriter writer,
        IAtlasLogger logger)
    {
        this.settings = settings;
        this.writer = writer;
        this.logger = logger;
    }

    /// <inheritdoc />
    public void Record(IdentifiedMatchProbabilityRequest request, IReadOnlyCollection<DonorInput> donors, DonorGenotypeSetBatchContext batchContext)
    {
        if (!settings.Enabled)
        {
            return;
        }

        // The record is built here, while the request and donors are still in use by the batch, and written on the
        // thread pool, so the batch never waits for the upload.
        GenotypeSetReadRecord record;
        try
        {
            record = BuildRecord(request, donors, batchContext, DateTimeOffset.UtcNow);
        }
        catch (Exception exception)
        {
            LogFailure(request?.SearchRequestId, exception);
            return;
        }

        _ = Task.Run(() => Write(record));
    }

    /// <summary>Writes the record and logs a failure instead of throwing. Internal so tests can await it.</summary>
    internal async Task Write(GenotypeSetReadRecord record)
    {
        try
        {
            await writer.Write(settings.BlobContainer, BlobName(record), record);
        }
        catch (Exception exception)
        {
            LogFailure(record.SearchRequestId, exception);
        }
    }

    internal static GenotypeSetReadRecord BuildRecord(
        IdentifiedMatchProbabilityRequest request,
        IReadOnlyCollection<DonorInput> donors,
        DonorGenotypeSetBatchContext batchContext,
        DateTimeOffset recordedAtUtc)
    {
        var allowedLoci = DonorGenotypeSetSourceResolver.AllowedLoci(request).Order();

        return new GenotypeSetReadRecord(
            recordedAtUtc,
            request.SearchRequestId,
            request.MatchingAlgorithmDataRefreshRecordId,
            string.Join(",", allowedLoci),
            batchContext.AllowedLociKey,
            batchContext.UsePrecomputedGenotypeSets,
            batchContext.BatchFallbackReason?.ToString(),
            donors.Select(d => (IReadOnlyCollection<int>)d.DonorIds.ToList()).ToList());
    }

    /// <summary>
    /// <c>{refresh record id}/{date}/{search request id}/{unique}.json</c>: the offline script reads one refresh record's
    /// folder while that database is still active.
    /// </summary>
    internal static string BlobName(GenotypeSetReadRecord record)
    {
        var refreshFolder = record.MatchingAlgorithmDataRefreshRecordId?.ToString() ?? UnknownRefreshRecordFolder;
        return $"{refreshFolder}/{record.RecordedAtUtc:yyyy-MM-dd}/{record.SearchRequestId}/{record.RecordedAtUtc:HHmmssfff}-{Guid.NewGuid():N}.json";
    }

    private void LogFailure(string searchRequestId, Exception exception)
    {
        try
        {
            logger.SendException(exception, LogLevel.Warn, new Dictionary<string, string>
            {
                { "EventName", RecordFailedEventName },
                { "SearchRequestId", searchRequestId },
            });
        }
        catch
        {
            // Logging must not fail the batch.
        }
    }
}

/// <summary>The default: records nothing. Hosts that read precomputed genotype sets replace it (see <c>RegisterGenotypeSetReadRecording</c>).</summary>
internal class NoOpGenotypeSetReadRecorder : IGenotypeSetReadRecorder
{
    public void Record(IdentifiedMatchProbabilityRequest request, IReadOnlyCollection<DonorInput> donors, DonorGenotypeSetBatchContext batchContext)
    {
    }
}
