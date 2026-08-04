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
using Atlas.MatchPrediction.Validators;
using FluentValidation;
using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.MatchPrediction;

// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable SuggestBaseTypeForParameter

namespace Atlas.MatchPrediction.Services.MatchProbability;

public interface IMatchProbabilityService
{
    /// <returns>
    /// The <see cref="MatchProbabilityResponse"/> together with the post-truncation donor imputed genotype count
    /// (see <see cref="MatchProbabilityResult"/>).
    /// </returns>
    public Task<MatchProbabilityResult> CalculateMatchProbability(
        SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput,
        SubjectGenotypeSet patientGenotypeSet);
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
        SubjectGenotypeSet patientGenotypeSet)
    {
        ArgumentNullException.ThrowIfNull(patientGenotypeSet);

        await new MatchProbabilityInputValidator().ValidateAndThrowAsync(singleDonorMatchProbabilityInput);

        matchProbabilityLoggingContext.Initialise(singleDonorMatchProbabilityInput);

        var frequencySets = await GetFrequencySets(singleDonorMatchProbabilityInput);
        var allowedLoci = LocusSettings.MatchPredictionLoci.Except(singleDonorMatchProbabilityInput.ExcludedLoci).ToHashSet();

        var matcherResult = await genotypeMatcher.MatchPatientDonorGenotypes(new GenotypeMatcherInput
            {
                PatientData = new SubjectData(singleDonorMatchProbabilityInput.PatientHla.ToPhenotypeInfo(), frequencySets.Patient),
                DonorData = new SubjectData(singleDonorMatchProbabilityInput.Donor.DonorHla.ToPhenotypeInfo(), frequencySets.Donor),
                PatientGenotypeSet = patientGenotypeSet,
                MatchPredictionParameters =
                    new MatchPredictionParameters(allowedLoci, singleDonorMatchProbabilityInput.MatchingAlgorithmHlaNomenclatureVersion)
            }
        );

        // Captured whether or not the phenotype is represented (0 when unrepresented), so it is read before the guard below.
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

            return new MatchProbabilityResult(unrepresentedResponse, donorGenotypeCount);
        }

        var response = CalculateMatchProbabilityFromMatcherResult(matcherResult, allowedLoci, frequencySets);
        return new MatchProbabilityResult(response, donorGenotypeCount);
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