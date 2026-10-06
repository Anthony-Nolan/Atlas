using System.Collections.Generic;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.Precompute;
using Atlas.MatchPrediction.Validators;
using FluentValidation;
using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;

// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable SuggestBaseTypeForParameter

namespace Atlas.MatchPrediction.Services.MatchProbability;

public interface IMatchProbabilityService
{
    /// <param name="singleDonorMatchProbabilityInput">The patient and one donor input.</param>
    /// <param name="patientGenotypeSet">The patient's genotype set, computed once per batch.</param>
    /// <param name="batchContext">
    /// The batch's precomputed genotype sets (ATL-221), resolved once per batch by
    /// <see cref="IDonorGenotypeSetSourceResolver"/>. Null computes the donor live, exactly as before precomputation.
    /// </param>
    /// <returns>
    /// The <see cref="MatchProbabilityResponse"/> together with the post-truncation donor imputed genotype count, where
    /// the donor's genotype set came from, and a live set to store for reuse, if any (see <see cref="MatchProbabilityResult"/>).
    /// </returns>
    public Task<MatchProbabilityResult> CalculateMatchProbability(
        SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput,
        SubjectGenotypeSet patientGenotypeSet,
        DonorGenotypeSetBatchContext batchContext = null);
}

internal class MatchProbabilityService : IMatchProbabilityService
{
    private readonly IGenotypeMatcher genotypeMatcher;
    private readonly IMatchProbabilityCalculator matchProbabilityCalculator;
    private readonly IHaplotypeFrequencyService haplotypeFrequencyService;
    private readonly IAtlasLogger logger;
    private readonly MatchProbabilityLoggingContext matchProbabilityLoggingContext;

    private record FrequencySets(SubjectFrequencySet Patient, SubjectFrequencySet Donor);

    public MatchProbabilityService(
        IMatchProbabilityCalculator matchProbabilityCalculator,
        IHaplotypeFrequencyService haplotypeFrequencyService,
        IGenotypeMatcher genotypeMatcher,
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
        MatchProbabilityLoggingContext matchProbabilityLoggingContext)
    {
        this.matchProbabilityCalculator = matchProbabilityCalculator;
        this.haplotypeFrequencyService = haplotypeFrequencyService;
        this.genotypeMatcher = genotypeMatcher;
        this.logger = logger;
        this.matchProbabilityLoggingContext = matchProbabilityLoggingContext;
    }

    public async Task<MatchProbabilityResult> CalculateMatchProbability(
        SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput,
        SubjectGenotypeSet patientGenotypeSet,
        DonorGenotypeSetBatchContext batchContext = null)
    {
        ArgumentNullException.ThrowIfNull(patientGenotypeSet);

        await new MatchProbabilityInputValidator().ValidateAndThrowAsync(singleDonorMatchProbabilityInput);

        matchProbabilityLoggingContext.Initialise(singleDonorMatchProbabilityInput);

        var frequencySets = await GetFrequencySets(singleDonorMatchProbabilityInput);
        var allowedLoci = LocusSettings.MatchPredictionLoci.Except(singleDonorMatchProbabilityInput.ExcludedLoci).ToHashSet();
        var donorFrequencySetId = frequencySets.Donor.FrequencySet.Id;
        var donorHla = singleDonorMatchProbabilityInput.Donor.DonorHla.ToPhenotypeInfo();

        // Compared with the set chosen just above, so a stored row computed with a since-replaced frequency set is
        // never used (StaleFrequencySet): not its payload, and not its IsUnrepresented flag.
        var genotypeSetSource = DonorGenotypeSetSource.PrecomputeDisabled;
        SubjectGenotypeSet precomputedDonorGenotypeSet = null;
        if (batchContext != null)
        {
            genotypeSetSource = batchContext.TryGetPrecomputedGenotypeSet(
                singleDonorMatchProbabilityInput.Donor.DonorIds,
                donorFrequencySetId,
                out precomputedDonorGenotypeSet);
        }

        var matcherResult = await genotypeMatcher.MatchPatientDonorGenotypes(new GenotypeMatcherInput
            {
                PatientData = new SubjectData(singleDonorMatchProbabilityInput.PatientHla.ToPhenotypeInfo(), frequencySets.Patient),
                DonorData = new SubjectData(donorHla, frequencySets.Donor),
                PatientGenotypeSet = patientGenotypeSet,
                DonorGenotypeSet = precomputedDonorGenotypeSet,
                MatchPredictionParameters =
                    new MatchPredictionParameters(allowedLoci, singleDonorMatchProbabilityInput.MatchingAlgorithmHlaNomenclatureVersion)
            }
        );

        var genotypeSetToStore = ShouldStore(genotypeSetSource, batchContext)
            ? ToStore(singleDonorMatchProbabilityInput.Donor, donorHla, donorFrequencySetId, matcherResult.DonorGenotypeSet)
            : null;

        // Capture the donor genotype count (0 when unrepresented), so it is read before the guard below.
        var donorGenotypeCount = matcherResult.DonorResult.GenotypeCount;

        if (matcherResult.PatientResult.IsUnrepresented || matcherResult.DonorResult.IsUnrepresented)
        {
            var unrepresentedResponse = new MatchProbabilityResponse(null, allowedLoci)
            {
                IsPatientPhenotypeUnrepresented = matcherResult.PatientResult.IsUnrepresented,
                IsDonorPhenotypeUnrepresented = matcherResult.DonorResult.IsUnrepresented,
                DonorHaplotypeFrequencySet = frequencySets.Donor.FrequencySet.ToClientHaplotypeFrequencySet(),
                PatientHaplotypeFrequencySet = frequencySets.Patient.FrequencySet.ToClientHaplotypeFrequencySet()
            };

            return new MatchProbabilityResult(unrepresentedResponse, donorGenotypeCount, genotypeSetSource, genotypeSetToStore);
        }

        var response = CalculateMatchProbabilityFromMatcherResult(matcherResult, allowedLoci, frequencySets);
        return new MatchProbabilityResult(response, donorGenotypeCount, genotypeSetSource, genotypeSetToStore);
    }

    /// <summary>
    /// A live set is stored only when the batch can store, and only for a donor that a stored row could have covered
    /// but did not: no row, or a row for another frequency set.
    /// </summary>
    /// <remarks>
    /// Not for <see cref="DonorGenotypeSetSource.DecodeFailed"/>: the bad row has the same key as the live set, so a
    /// store would find it and change nothing. The next Data Refresh rebuilds it. Not for
    /// <see cref="DonorGenotypeSetSource.TypingChanged"/>: the donor's current typing is not the one computed here.
    /// </remarks>
    private static bool ShouldStore(DonorGenotypeSetSource source, DonorGenotypeSetBatchContext batchContext) =>
        batchContext is { CanStore: true }
        && source is DonorGenotypeSetSource.NoRow or DonorGenotypeSetSource.StaleFrequencySet;

    /// <remarks>
    /// Encoded here, per donor, so a batch holds only payload bytes until it stores them, not every donor's genotypes.
    /// Null data for an unrepresented set is the same policy as the data refresh precompute.
    /// Storing is best effort, so a set that cannot be encoded is simply not stored: it must not fail the donor's result.
    /// </remarks>
    private DonorGenotypeSetToStore ToStore(
        DonorInput donor,
        PhenotypeInfo<string> donorHla,
        int donorFrequencySetId,
        SubjectGenotypeSet donorGenotypeSet)
    {
        try
        {
            return new DonorGenotypeSetToStore(
                donor.DonorIds.ToList(),
                donorHla,
                donor.DonorFrequencySetMetadata?.RegistryCode,
                donor.DonorFrequencySetMetadata?.EthnicityCode,
                donorFrequencySetId,
                donorGenotypeSet.IsUnrepresented,
                donorGenotypeSet.IsUnrepresented ? null : SubjectGenotypeSetPayload.Encode(donorGenotypeSet));
        }
        catch (Exception exception)
        {
            logger.SendException(exception, LogLevel.Warn, new Dictionary<string, string>
            {
                { "EventName", "Precomputed genotype set encode failed" },
                { "DonorIds", string.Join(",", donor.DonorIds) },
            });
            return null;
        }
    }

    private async Task<FrequencySets> GetFrequencySets(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput)
    {
        var frequencySets = await haplotypeFrequencyService.GetHaplotypeFrequencySets(
            singleDonorMatchProbabilityInput.Donor.DonorFrequencySetMetadata,
            singleDonorMatchProbabilityInput.PatientFrequencySetMetadata
        );

        return new FrequencySets(
            new SubjectFrequencySet(frequencySets.PatientSet, "patient"),
            new SubjectFrequencySet(frequencySets.DonorSet, "donor")
        );
    }

    private MatchProbabilityResponse CalculateMatchProbabilityFromMatcherResult(
        GenotypeMatcherResult matcherResult,
        HashSet<Locus> allowedLoci,
        FrequencySets frequencySets)
    {
        using (logger.RunTimed("Calculate match probability", LogLevel.Verbose))
        {
            var matchProbability = matchProbabilityCalculator.CalculateMatchProbability(
                matcherResult.PatientResult.SumOfLikelihoods,
                matcherResult.DonorResult.SumOfLikelihoods,
                matcherResult.GenotypeMatchDetails,
                allowedLoci
            );

            matchProbability.PatientHaplotypeFrequencySet = frequencySets.Patient.FrequencySet.ToClientHaplotypeFrequencySet();
            matchProbability.DonorHaplotypeFrequencySet = frequencySets.Donor.FrequencySet.ToClientHaplotypeFrequencySet();

            return matchProbability;
        }
    }
}