using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;

namespace Atlas.MatchPrediction.Services.Precompute;

/// <summary>One donor input's part in a batch: how many donor ids it covers, where its set came from, and what to store.</summary>
public sealed record DonorGenotypeSetBatchOutcome(int DonorIdCount, DonorGenotypeSetSource Source, DonorGenotypeSetToStore GenotypeSetToStore);

public interface IDonorGenotypeSetBatchCompleter
{
    /// <summary>
    /// Stores the batch's live-computed donor sets for reuse, as best effort, then sends the batch's one
    /// <see cref="DonorGenotypeSetBatchCompleter.UsageEventName"/> event. Never throws.
    /// </summary>
    /// <remarks>Call after the batch's results are uploaded, so the store can only ever delay the next batch, not this one's results.</remarks>
    Task Complete(
        IdentifiedMatchProbabilityRequest request,
        DonorGenotypeSetBatchContext batchContext,
        IReadOnlyCollection<DonorGenotypeSetBatchOutcome> outcomes);
}

internal class DonorGenotypeSetBatchCompleter : IDonorGenotypeSetBatchCompleter
{
    internal const string UsageEventName = "Precomputed genotype sets used";
    internal const string StoreFailedEventName = "Precomputed genotype set store failed";

    private readonly IPrecomputedDonorGenotypeSetWriter writer;
    private readonly IAtlasLogger logger;

    /// <remarks>
    /// The plain logger, not the match probability one: that one stamps every event with the last donor's ids and HLA,
    /// which would be wrong on a batch-level event. <c>SearchRequestId</c> is added explicitly instead.
    /// </remarks>
    public DonorGenotypeSetBatchCompleter(IPrecomputedDonorGenotypeSetWriter writer, IAtlasLogger logger)
    {
        this.writer = writer;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task Complete(
        IdentifiedMatchProbabilityRequest request,
        DonorGenotypeSetBatchContext batchContext,
        IReadOnlyCollection<DonorGenotypeSetBatchOutcome> outcomes)
    {
        var storeResult = await StoreBestEffort(request, batchContext, outcomes);
        SendUsageEvent(request, batchContext, outcomes, storeResult);
    }

    private async Task<DonorGenotypeSetStoreResult> StoreBestEffort(
        IdentifiedMatchProbabilityRequest request,
        DonorGenotypeSetBatchContext batchContext,
        IReadOnlyCollection<DonorGenotypeSetBatchOutcome> outcomes)
    {
        var genotypeSetsToStore = outcomes.Select(o => o.GenotypeSetToStore).Where(s => s != null).ToList();
        if (genotypeSetsToStore.Count == 0 || !batchContext.CanStore)
        {
            return DonorGenotypeSetStoreResult.None;
        }

        try
        {
            return await writer.Store(
                genotypeSetsToStore,
                DonorGenotypeSetSourceResolver.AllowedLoci(request),
                // CanStore guarantees the record id.
                batchContext.MatchingAlgorithmDataRefreshRecordId!.Value);
        }
        catch (Exception exception)
        {
            // Storing only saves the next search some work. The results of this one are already uploaded, so a
            // failure here must never fail the batch.
            LogStoreFailure(request, genotypeSetsToStore, exception);
            return DonorGenotypeSetStoreResult.None;
        }
    }

    private void LogStoreFailure(IdentifiedMatchProbabilityRequest request, IReadOnlyCollection<DonorGenotypeSetToStore> genotypeSets, Exception exception)
    {
        try
        {
            var props = new Dictionary<string, string>
            {
                { "SearchRequestId", request.SearchRequestId },
                { "DonorIdCount", genotypeSets.Sum(s => s.DonorIds.Count).ToString(CultureInfo.InvariantCulture) },
                { "ExceptionType", exception.GetType().FullName },
                { "ExceptionMessage", exception.Message },
            };
            logger.SendEvent(StoreFailedEventName, LogLevel.Warn, props);
            logger.SendException(exception, LogLevel.Warn, new Dictionary<string, string>
            {
                { "EventName", StoreFailedEventName },
                { "SearchRequestId", request.SearchRequestId },
            });
        }
        catch
        {
            // Logging must not fail the batch either.
        }
    }

    /// <remarks>
    /// One event per batch, not per donor, to keep the volume low. Summing a search's events gives the search's totals.
    /// Donor counts are of donor inputs (one per distinct phenotype and frequency set metadata, which is the unit that is
    /// imputed); <c>DonorIdCount</c> is the number of donors they cover.
    /// </remarks>
    private void SendUsageEvent(
        IdentifiedMatchProbabilityRequest request,
        DonorGenotypeSetBatchContext batchContext,
        IReadOnlyCollection<DonorGenotypeSetBatchOutcome> outcomes,
        DonorGenotypeSetStoreResult storeResult)
    {
        try
        {
            var props = new Dictionary<string, string>
            {
                { "SearchRequestId", request.SearchRequestId },
                { "AllowedLociKey", batchContext.AllowedLociKey },
                { "UsePrecomputedGenotypeSets", batchContext.UsePrecomputedGenotypeSets.ToString() },
                { "UsePrecomputedGenotypeSetsSource", batchContext.UsePrecomputedGenotypeSetsSource.ToString() },
                { "MatchingAlgorithmDataRefreshRecordId", batchContext.MatchingAlgorithmDataRefreshRecordId?.ToString(CultureInfo.InvariantCulture) },
            };

            var metrics = new Dictionary<string, double>
            {
                { "DonorCount", outcomes.Count },
                { "DonorIdCount", outcomes.Sum(o => o.DonorIdCount) },
                { "PrecomputedCount", outcomes.Count(o => o.Source == DonorGenotypeSetSource.Precomputed) },
                { "StoredCount", storeResult.StoredCount },
                { "StoreSkippedHlaChanged", storeResult.SkippedHlaChangedCount },
                { "StoreSkippedDatabaseChanged", storeResult.SkippedDatabaseChangedCount },
            };

            foreach (var reason in Enum.GetValues<DonorGenotypeSetSource>().Where(s => s != DonorGenotypeSetSource.Precomputed))
            {
                metrics[reason.ToString()] = outcomes.Count(o => o.Source == reason);
            }

            logger.SendEvent(UsageEventName, LogLevel.Info, props, metrics);
        }
        catch
        {
            // Logging must not fail the batch.
        }
    }
}
