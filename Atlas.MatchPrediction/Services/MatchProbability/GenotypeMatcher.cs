using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.MatchCalculation;
using LoggingStopwatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using static Atlas.Common.Maths.Combinations;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public class GenotypeMatcherInput
    {
        public SubjectData PatientData { get; set; }
        public SubjectData DonorData { get; set; }
        public HashSet<Locus> AllowedLoci { get; set; }
    }

    public class GenotypeMatcherResult
    {
        public SubjectResult PatientResult { get; set; }
        public SubjectResult DonorResult { get; set; }
        public IEnumerable<GenotypeMatchDetails> GenotypeMatchDetails { get; set; }

        public class SubjectResult
        {
            public bool IsUnrepresented { get; set; }
            public decimal SumOfLikelihoods { get; set; }

            public SubjectResult(bool isUnrepresented, decimal sumOfLikelihoods)
            {
                IsUnrepresented = isUnrepresented;
                SumOfLikelihoods = sumOfLikelihoods;
            }
        }
    }

    public interface IGenotypeMatcher
    {
        Task<GenotypeMatcherResult> MatchPatientDonorGenotypes(GenotypeMatcherInput input);
    }

    internal class GenotypeMatcher : IGenotypeMatcher
    {
        private readonly IGenotypeImputationService genotypeImputer;
        private readonly IGenotypeConverter genotypeConverter;
        private readonly IMatchCalculationService matchCalculationService;
        private readonly ILogger logger;

        private record GenotypeSet(bool IsUnrepresented, ICollection<GenotypeAtDesiredResolutions> Genotypes, decimal SumOfLikelihoods);

        public GenotypeMatcher(
            IGenotypeImputationService genotypeImputer,
            IGenotypeConverter genotypeConverter,
            IMatchCalculationService matchCalculationService,
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger)
        {
            this.genotypeImputer = genotypeImputer;
            this.genotypeConverter = genotypeConverter;
            this.matchCalculationService = matchCalculationService;
            this.logger = logger;
        }

        /// <inheritdoc />
        public async Task<GenotypeMatcherResult> MatchPatientDonorGenotypes(GenotypeMatcherInput input)
        {
            var patientGenotypeSet = await GetGenotypeSetResultForMatchCounting(input.PatientData, input.AllowedLoci);
            var donorGenotypeSet = await GetGenotypeSetResultForMatchCounting(input.DonorData, input.AllowedLoci);

            if (patientGenotypeSet.IsUnrepresented || donorGenotypeSet.IsUnrepresented)
            {
                return new GenotypeMatcherResult
                {
                    PatientResult = new GenotypeMatcherResult.SubjectResult(patientGenotypeSet.IsUnrepresented, patientGenotypeSet.SumOfLikelihoods),
                    DonorResult = new GenotypeMatcherResult.SubjectResult(donorGenotypeSet.IsUnrepresented, donorGenotypeSet.SumOfLikelihoods)
                };
            }

            var pdpCount = NumberOfPairsOfCartesianProduct(donorGenotypeSet.Genotypes, patientGenotypeSet.Genotypes);
            logger.SendTrace($"Patient/donor pairs: {pdpCount:n0}");

            var allPatientDonorCombinations = CombineGenotypes(patientGenotypeSet.Genotypes, donorGenotypeSet.Genotypes);

            using var matchCountLogger = MatchCountLogger(pdpCount);

            return new GenotypeMatcherResult
            {
                PatientResult = new GenotypeMatcherResult.SubjectResult(false, patientGenotypeSet.SumOfLikelihoods),
                DonorResult = new GenotypeMatcherResult.SubjectResult(false, donorGenotypeSet.SumOfLikelihoods),
                GenotypeMatchDetails = CalculatePairsMatchCounts(allPatientDonorCombinations, input.AllowedLoci, matchCountLogger)
            };
        }

        private async Task<GenotypeSet> GetGenotypeSetResultForMatchCounting(SubjectData subjectData, HashSet<Locus> allowedLoci)
        {
            var imputedGenotypes = await genotypeImputer.Impute(new ImputationInput
            {
                SubjectData = subjectData,
                AllowedMatchPredictionLoci = allowedLoci
            });

            if (imputedGenotypes.Genotypes.IsNullOrEmpty())
            {
                return new GenotypeSet(true, null, 0m);
            }

            return new GenotypeSet(false,
                await genotypeConverter.ConvertGenotypes(
                    imputedGenotypes.Genotypes,
                    subjectData.SubjectFrequencySet.SubjectLogDescription,
                    imputedGenotypes.GenotypeLikelihoods,
                    subjectData.SubjectFrequencySet.FrequencySet.HlaNomenclatureVersion),
                SumOfLikelihoods(imputedGenotypes));
        }

        private IEnumerable<Tuple<GenotypeAtDesiredResolutions, GenotypeAtDesiredResolutions>> CombineGenotypes(
            IEnumerable<GenotypeAtDesiredResolutions> patientGenotypes,
            ICollection<GenotypeAtDesiredResolutions> donorGenotypes)
        {
            using (logger.RunTimed("Combining patient/donor genotypes", LogLevel.Verbose))
            {
                var combinations = patientGenotypes.SelectMany(patientHla =>
                    donorGenotypes.Select(donorHla =>
                        new Tuple<GenotypeAtDesiredResolutions, GenotypeAtDesiredResolutions>(patientHla, donorHla)));

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

        private static decimal SumOfLikelihoods(ImputedGenotypes imputedGenotypes) => imputedGenotypes.GenotypeLikelihoods.Values.SumDecimals();
    }
}