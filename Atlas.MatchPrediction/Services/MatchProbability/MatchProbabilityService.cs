using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Validators;
using FluentValidation;
using LoggingStopwatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using static Atlas.Common.Maths.Combinations;

// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable SuggestBaseTypeForParameter

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public interface IMatchProbabilityService
    {
        public Task<MatchProbabilityResponse> CalculateMatchProbability(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput);
    }

    internal class MatchProbabilityService : IMatchProbabilityService
    {
        private readonly IGenotypeConverter genotypeConverter;
        private readonly IGenotypeImputationService genotypeImputationService;
        private readonly IMatchCalculationService matchCalculationService;
        private readonly IMatchProbabilityCalculator matchProbabilityCalculator;
        private readonly IHaplotypeFrequencyService haplotypeFrequencyService;
        private readonly ILogger logger;
        private readonly MatchProbabilityLoggingContext matchProbabilityLoggingContext;

        private record GenotypeSetResult(bool IsUnrepresented, ICollection<GenotypeAtDesiredResolutions> GenotypeSet, decimal SumOfLikelihoods);

        public MatchProbabilityService(
            IGenotypeConverter genotypeConverter,
            IMatchCalculationService matchCalculationService,
            IMatchProbabilityCalculator matchProbabilityCalculator,
            IHaplotypeFrequencyService haplotypeFrequencyService,
            IGenotypeImputationService genotypeImputationService,
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
            MatchProbabilityLoggingContext matchProbabilityLoggingContext)
        {
            this.genotypeConverter = genotypeConverter;
            this.genotypeImputationService = genotypeImputationService;
            this.matchCalculationService = matchCalculationService;
            this.matchProbabilityCalculator = matchProbabilityCalculator;
            this.haplotypeFrequencyService = haplotypeFrequencyService;
            this.logger = logger;
            this.matchProbabilityLoggingContext = matchProbabilityLoggingContext;
        }

        public async Task<MatchProbabilityResponse> CalculateMatchProbability(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput)
        {
            await new MatchProbabilityInputValidator().ValidateAndThrowAsync(singleDonorMatchProbabilityInput);

            matchProbabilityLoggingContext.Initialise(singleDonorMatchProbabilityInput);

            var frequencySets = await haplotypeFrequencyService.GetHaplotypeFrequencySets(
                singleDonorMatchProbabilityInput.Donor.DonorFrequencySetMetadata,
                singleDonorMatchProbabilityInput.PatientFrequencySetMetadata
            );

            var allowedLoci = LocusSettings.MatchPredictionLoci.Except(singleDonorMatchProbabilityInput.ExcludedLoci).ToHashSet();

            var patientGenotypeSet = await GetGenotypeSetForMatchCalculation(new ImputationInput
            {
                HlaTyping = singleDonorMatchProbabilityInput.PatientHla.ToPhenotypeInfo(),
                AllowedMatchPredictionLoci = allowedLoci,
                FrequencySet = frequencySets.PatientSet,
                SubjectLogDescription = "patient"
            });

            var donorGenotypeSet = await GetGenotypeSetForMatchCalculation(new ImputationInput
            {
                HlaTyping = singleDonorMatchProbabilityInput.Donor.DonorHla.ToPhenotypeInfo(),
                AllowedMatchPredictionLoci = allowedLoci,
                FrequencySet = frequencySets.DonorSet,
                SubjectLogDescription = "donor"
            });

            if (patientGenotypeSet.IsUnrepresented || donorGenotypeSet.IsUnrepresented)
            {
                return new MatchProbabilityResponse(null, allowedLoci)
                {
                    IsDonorPhenotypeUnrepresented = donorGenotypeSet.IsUnrepresented,
                    IsPatientPhenotypeUnrepresented = patientGenotypeSet.IsUnrepresented,
                    DonorHaplotypeFrequencySet = frequencySets.DonorSet.ToClientHaplotypeFrequencySet(),
                    PatientHaplotypeFrequencySet = frequencySets.PatientSet.ToClientHaplotypeFrequencySet()
                };
            }

            return CalculateMatchProbabilityFromGenotypeSets(patientGenotypeSet, donorGenotypeSet, allowedLoci, frequencySets);
        }

        private async Task<GenotypeSetResult> GetGenotypeSetForMatchCalculation(ImputationInput input)
        {
            var imputedGenotypes = await genotypeImputationService.Impute(input);

            if (imputedGenotypes.Genotypes.IsNullOrEmpty())
            {
                return new GenotypeSetResult(true, new List<GenotypeAtDesiredResolutions>(), 0);
            }

            var convertedGenotypes = await genotypeConverter.ConvertGenotypes(
                imputedGenotypes.Genotypes,
                input.SubjectLogDescription,
                imputedGenotypes.GenotypeLikelihoods,
                input.FrequencySet.HlaNomenclatureVersion);

            var sumOfLikelihoods = imputedGenotypes.GenotypeLikelihoods.Values.SumDecimals();

            return new GenotypeSetResult(false, convertedGenotypes, sumOfLikelihoods);
        }

        private IEnumerable<Tuple<GenotypeAtDesiredResolutions, GenotypeAtDesiredResolutions>> CombineGenotypes(
            ICollection<GenotypeAtDesiredResolutions> patientGenotypes,
            ICollection<GenotypeAtDesiredResolutions> donorGenotypes)
        {
            using (logger.RunTimed("Combining patient/donor genotypes", LogLevel.Verbose))
            {
                var combinations = patientGenotypes.SelectMany(patientHla =>
                    donorGenotypes.Select(donorHla =>
                        new Tuple<GenotypeAtDesiredResolutions, GenotypeAtDesiredResolutions>(patientHla, donorHla)));

                logger.SendTrace($"Patient/donor pairs: {NumberOfPairsOfCartesianProduct(patientGenotypes, donorGenotypes):n0}");

                return combinations;
            }
        }

        private ILongOperationLoggingStopwatch MatchCountLogger(long patientDonorPairCount) => logger.RunLongOperationWithTimer(
            "Calculate match counts.",
            new LongLoggingSettings
            {
                ExpectedNumberOfIterations = patientDonorPairCount,
                // The higher this number, the slower the algorithm will be. Every 500,000 seems to be a good balance opf performance vs. Information.
                InnerOperationLoggingPeriod = 500_000
            },
            LogLevel.Verbose
        );

        // This needs to return an IEnumerable to enable lazy evaluation - the list of combinations will be very long, so we do not want to enumerate 
        // it more than once, so we leave it as an IEnumerable until match probability aggregation.
        private IEnumerable<GenotypeMatchDetails> CalculatePairsMatchCounts(
            IEnumerable<Tuple<GenotypeAtDesiredResolutions, GenotypeAtDesiredResolutions>> allPatientDonorCombinations,
            ISet<Locus> allowedLoci,
            ILongOperationLoggingStopwatch stopwatch)
        {
            return allPatientDonorCombinations
                .Select(pd =>
                {
                    using (stopwatch.TimeInnerOperation())
                    {
                        var (patient, donor) = pd;
                        return new GenotypeMatchDetails
                        {
                            AvailableLoci = allowedLoci,
                            DonorGenotype = donor.HaplotypeResolution,
                            DonorGenotypeLikelihood = donor.GenotypeLikelihood,
                            PatientGenotype = patient.HaplotypeResolution,
                            PatientGenotypeLikelihood = patient.GenotypeLikelihood,
                            MatchCounts = matchCalculationService.CalculateMatchCounts_Fast(
                                patient.StringMatchableResolution,
                                donor.StringMatchableResolution,
                                allowedLoci
                            )
                        };
                    }
                });
        }

        private MatchProbabilityResponse CalculateMatchProbabilityFromGenotypeSets(
            GenotypeSetResult patientGenotypeSet,
            GenotypeSetResult donorGenotypeSet,
            HashSet<Locus> allowedLoci,
            HaplotypeFrequencySetResponse frequencySets)
        {
            var allPatientDonorCombinations = CombineGenotypes(patientGenotypeSet.GenotypeSet, donorGenotypeSet.GenotypeSet);

            using (var matchCountLogger = MatchCountLogger(NumberOfPairsOfCartesianProduct(donorGenotypeSet.GenotypeSet, patientGenotypeSet.GenotypeSet)))
            {
                var patientDonorMatchDetails = CalculatePairsMatchCounts(allPatientDonorCombinations, allowedLoci, matchCountLogger);

                using (logger.RunTimed("Calculate match probability", LogLevel.Verbose))
                {
                    var matchProbability = matchProbabilityCalculator.CalculateMatchProbability(
                        patientGenotypeSet.SumOfLikelihoods,
                        donorGenotypeSet.SumOfLikelihoods,
                        patientDonorMatchDetails,
                        allowedLoci
                    );

                    matchProbability.DonorHaplotypeFrequencySet = frequencySets.DonorSet.ToClientHaplotypeFrequencySet();
                    matchProbability.PatientHaplotypeFrequencySet = frequencySets.PatientSet.ToClientHaplotypeFrequencySet();

                    return matchProbability;
                }
            }
        }
    }
}