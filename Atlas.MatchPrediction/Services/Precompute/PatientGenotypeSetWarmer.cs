using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;

namespace Atlas.MatchPrediction.Services.Precompute;

public interface IPatientGenotypeSetWarmer
{
    /// <summary>
    /// Makes sure the search's patient genotype set is stored, computing and storing it if it is not, and returns its
    /// key for the batches (<see cref="IdentifiedMatchProbabilityRequest.PatientGenotypeSetKey"/>). Call once per search,
    /// before the batches are uploaded. Never throws, and sends one <see cref="PatientGenotypeSetWarmer.WarmedEventName"/>
    /// event.
    /// </summary>
    /// <returns>
    /// The key; null when the search does not use stored sets, or when the set is not stored. Each batch then computes
    /// the patient live, as before.
    /// </returns>
    /// <remarks>Safe to repeat, so a retried activity is fine: a stored set is found, not stored again.</remarks>
    Task<PatientGenotypeSetKey> Warm(IdentifiedMatchProbabilityRequest request);
}

internal class PatientGenotypeSetWarmer : IPatientGenotypeSetWarmer
{
    internal const string WarmedEventName = "Precomputed patient genotype set warmed";

    internal enum WarmResult
    {
        /// <summary>The set was already stored.</summary>
        Hit,

        /// <summary>The set was computed and stored.</summary>
        Created,

        /// <summary>Nothing was looked up or stored; the reason says why.</summary>
        Skipped,

        /// <summary>An error stopped the step. It is logged; the batches compute the patient live.</summary>
        Failed
    }

    private readonly PrecomputedGenotypeSetSettings settings;
    private readonly IHaplotypeFrequencyLookupService haplotypeFrequencyService;
    private readonly IGenotypeSetService genotypeSetService;
    private readonly IPrecomputedDonorGenotypeSetReader reader;
    private readonly IPrecomputedDonorGenotypeSetWriter writer;
    private readonly IAtlasLogger logger;

    public PatientGenotypeSetWarmer(
        PrecomputedGenotypeSetSettings settings,
        IHaplotypeFrequencyLookupService haplotypeFrequencyService,
        IGenotypeSetService genotypeSetService,
        IPrecomputedDonorGenotypeSetReader reader,
        IPrecomputedDonorGenotypeSetWriter writer,
        IAtlasLogger logger)
    {
        this.settings = settings;
        this.haplotypeFrequencyService = haplotypeFrequencyService;
        this.genotypeSetService = genotypeSetService;
        this.reader = reader;
        this.writer = writer;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<PatientGenotypeSetKey> Warm(IdentifiedMatchProbabilityRequest request)
    {
        PatientGenotypeSetKey key = null;
        try
        {
            // The same decision the batches make. A batch decides again, so a mode change after this step still wins.
            var (usePrecomputedGenotypeSets, _) = DonorGenotypeSetSourceResolver.Decide(settings.Mode, request.UsePrecomputedGenotypeSets);
            if (!usePrecomputedGenotypeSets)
            {
                SendEvent(request, WarmResult.Skipped, nameof(DonorGenotypeSetSource.PrecomputeDisabled), null, null);
                return null;
            }

            var patientFrequencySet = await haplotypeFrequencyService.GetSingleHaplotypeFrequencySet(
                request.PatientFrequencySetMetadata ?? new FrequencySetMetadata());
            var allowedLoci = DonorGenotypeSetSourceResolver.AllowedLoci(request);

            var lookup = await reader.FindPatientGenotypeSet(
                request.PatientHla.ToPhenotypeInfo(),
                patientFrequencySet.Id,
                allowedLoci,
                request.MatchingAlgorithmDataRefreshRecordId);
            if (lookup.UnavailableReason != null)
            {
                SendEvent(request, WarmResult.Skipped, lookup.UnavailableReason.ToString(), null, null);
                return null;
            }

            key = lookup.Key;
            if (lookup.Exists)
            {
                SendEvent(request, WarmResult.Hit, null, key, null);
                return key;
            }

            // Computed exactly as a batch computes the patient live, with the frequency set the key was built with.
            var genotypeSet = await genotypeSetService.GetPatientGenotypeSet(new SingleDonorMatchProbabilityInput(request), patientFrequencySet);
            var stored = await writer.StorePatientGenotypeSet(
                key,
                genotypeSet.IsUnrepresented,
                genotypeSet.IsUnrepresented ? null : SubjectGenotypeSetPayload.Encode(genotypeSet),
                // The lookup ran, so the record id is set.
                request.MatchingAlgorithmDataRefreshRecordId!.Value);

            if (!stored)
            {
                SendEvent(request, WarmResult.Skipped, nameof(DonorGenotypeSetSource.ActiveDatabaseChanged), key, genotypeSet.Genotypes.Count);
                return null;
            }

            SendEvent(request, WarmResult.Created, null, key, genotypeSet.Genotypes.Count);
            return key;
        }
        catch (Exception exception)
        {
            // The step only saves the batches some work, so a failure must never fail the search.
            LogFailure(request, key, exception);
            return null;
        }
    }

    private void SendEvent(
        IdentifiedMatchProbabilityRequest request,
        WarmResult result,
        string reason,
        PatientGenotypeSetKey key,
        int? patientGenotypeCount,
        Exception exception = null)
    {
        try
        {
            var props = new Dictionary<string, string>
            {
                { "SearchRequestId", request.SearchRequestId },
                { "Result", result.ToString() },
                { "Reason", reason },
                { "AllowedLociKey", key?.AllowedLociKey },
                { "HaplotypeFrequencySetId", key?.HaplotypeFrequencySetId.ToString(CultureInfo.InvariantCulture) },
                { "PrecomputeMode", settings.Mode.ToString() },
                { "MatchingAlgorithmDataRefreshRecordId", request.MatchingAlgorithmDataRefreshRecordId?.ToString(CultureInfo.InvariantCulture) },
            };
            if (exception != null)
            {
                props["ExceptionType"] = exception.GetType().FullName;
                props["ExceptionMessage"] = exception.Message;
            }

            var metrics = new Dictionary<string, double>();
            if (patientGenotypeCount != null)
            {
                metrics["PatientGenotypeCount"] = patientGenotypeCount.Value;
            }

            logger.SendEvent(WarmedEventName, exception == null ? LogLevel.Info : LogLevel.Warn, props, metrics);
        }
        catch
        {
            // Logging must not fail the search.
        }
    }

    private void LogFailure(IdentifiedMatchProbabilityRequest request, PatientGenotypeSetKey key, Exception exception)
    {
        SendEvent(request, WarmResult.Failed, null, key, null, exception);
        try
        {
            logger.SendException(exception, LogLevel.Warn, new Dictionary<string, string>
            {
                { "EventName", WarmedEventName },
                { "SearchRequestId", request.SearchRequestId },
            });
        }
        catch
        {
            // Logging must not fail the search.
        }
    }
}
