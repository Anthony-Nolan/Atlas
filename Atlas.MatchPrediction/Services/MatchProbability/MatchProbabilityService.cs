using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Validators;
using FluentValidation;
using LoggingStopwatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Atlas.Common.Maths.Combinations;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;
using StringGenotype = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;
using TypedGenotype = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<Atlas.MatchPrediction.ExternalInterface.Models.HlaAtKnownTypingCategory>;

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
        private const string LoggingPrefix = "MatchPrediction: ";

        private readonly ICompressedPhenotypeExpander compressedPhenotypeExpander;
        private readonly IGenotypeLikelihoodService genotypeLikelihoodService;
        private readonly IGenotypeConverter genotypeConverter;
        private readonly IMatchCalculationService matchCalculationService;
        private readonly IMatchProbabilityCalculator matchProbabilityCalculator;
        private readonly IHaplotypeFrequencyService haplotypeFrequencyService;
        private readonly ILogger logger;
        private readonly MatchProbabilityLoggingContext matchProbabilityLoggingContext;

        private record GenotypeSetResult(bool IsUnrepresented, ICollection<GenotypeAtDesiredResolutions> GenotypeSet, decimal SumOfLikelihoods);

        public MatchProbabilityService(
            ICompressedPhenotypeExpander compressedPhenotypeExpander,
            IGenotypeLikelihoodService genotypeLikelihoodService,
            IGenotypeConverter genotypeConverter,
            IMatchCalculationService matchCalculationService,
            IMatchProbabilityCalculator matchProbabilityCalculator,
            IHaplotypeFrequencyService haplotypeFrequencyService,
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
            MatchProbabilityLoggingContext matchProbabilityLoggingContext)
        {
            this.compressedPhenotypeExpander = compressedPhenotypeExpander;
            this.genotypeLikelihoodService = genotypeLikelihoodService;
            this.genotypeConverter = genotypeConverter;
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
            var patientGenotypeSet = await GetGenotypeSet(
                singleDonorMatchProbabilityInput.PatientHla.ToPhenotypeInfo(), allowedLoci, frequencySets.PatientSet, "patient");
            var donorGenotypeSet = await GetGenotypeSet(
                singleDonorMatchProbabilityInput.Donor.DonorHla.ToPhenotypeInfo(), allowedLoci, frequencySets.DonorSet, "donor");

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

        private async Task<GenotypeSetResult> GetGenotypeSet(
            StringGenotype stringGenotype,
            HashSet<Locus> allowedLoci,
            HaplotypeFrequencySet frequencySet,
            string subjectLogDescription)
        {
            var genotypes = await ExpandToGenotypes(
                stringGenotype,
                allowedLoci,
                frequencySet,
                subjectLogDescription
            );

            if (genotypes.IsNullOrEmpty())
            {
                logger.SendTrace($"{LoggingPrefix}{subjectLogDescription} genotype unrepresented.", LogLevel.Verbose);
                return new GenotypeSetResult(true, new List<GenotypeAtDesiredResolutions>(), 0);
            }

            var truncatedSet = await GetTruncatedGenotypeSet(allowedLoci, frequencySet, genotypes);
            
            var convertedGenotypes = await genotypeConverter.ConvertGenotypes(
                truncatedSet.Genotypes,
                subjectLogDescription,
                truncatedSet.GenotypeLikelihoods,
                frequencySet.HlaNomenclatureVersion);

            var sumOfLikelihoods = truncatedSet.GenotypeLikelihoods.Values.SumDecimals();

            return new GenotypeSetResult(false, convertedGenotypes, sumOfLikelihoods);
        }

        private async Task<ISet<TypedGenotype>> ExpandToGenotypes(
            StringGenotype phenotype,
            ISet<Locus> allowedLoci,
            HaplotypeFrequencySet frequencySet,
            string subjectLogDescription)
        {
            var haplotypeFrequencies = await haplotypeFrequencyService.GetAllHaplotypeFrequencies(frequencySet.Id);
            using (logger.RunTimed($"{LoggingPrefix}Expand {subjectLogDescription} phenotype", LogLevel.Verbose))
            {
                var groupedFrequencies = haplotypeFrequencies
                    .GroupBy(f => f.Value.TypingCategory)
                    .Select(g =>
                        new KeyValuePair<HaplotypeTypingCategory, IReadOnlyCollection<LociInfo<string>>>(g.Key, g.Select(f => f.Value.Hla).ToList())
                    )
                    .ToDictionary();

                return await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                    new ExpandCompressedPhenotypeInput
                    {
                        Phenotype = phenotype,
                        AllowedLoci = allowedLoci,
                        HlaNomenclatureVersion = frequencySet.HlaNomenclatureVersion,
                        AllHaplotypes = new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
                        {
                            GGroup = groupedFrequencies.GetValueOrDefault(HaplotypeTypingCategory.GGroup, new List<LociInfo<string>>()),
                            PGroup = groupedFrequencies.GetValueOrDefault(HaplotypeTypingCategory.PGroup, new List<LociInfo<string>>()),
                            SmallGGroup = groupedFrequencies.GetValueOrDefault(HaplotypeTypingCategory.SmallGGroup, new List<LociInfo<string>>()),
                        }
                    }
                );
            }
        }

        private async Task<TruncatedGenotypeSet> GetTruncatedGenotypeSet(HashSet<Locus> allowedLoci, HaplotypeFrequencySet frequencySet, ISet<TypedGenotype> genotypes)
        {
            var genotypeLikelihoods = await CalculateGenotypeLikelihoods(genotypes, frequencySet, allowedLoci);
            return ExpandedGenotypeTruncater.TruncateGenotypes(genotypeLikelihoods, genotypes);
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

        private async Task<Dictionary<StringGenotype, decimal>> CalculateGenotypeLikelihoods(
            ISet<TypedGenotype> genotypes,
            HaplotypeFrequencySet frequencySet,
            ISet<Locus> allowedLoci)
        {
            using (logger.RunTimed($"{LoggingPrefix}Calculate likelihoods for genotypes", LogLevel.Verbose))
            {
                var genotypeLikelihoods = new List<KeyValuePair<StringGenotype, decimal>>();

                var stringGenotypes = genotypes.Select(g => g.ToHlaNames()).ToHashSet();

                // If there is no ambiguity for an input genotype, we do not need to use haplotype frequencies to work out the likelihood of said genotype - it is already guaranteed! 
                if (stringGenotypes.Count == 1)
                {
                    genotypeLikelihoods.Add(new KeyValuePair<StringGenotype, decimal>(stringGenotypes.Single(), 1));
                }
                else
                {
                    foreach (var genotype in stringGenotypes)
                    {
                        genotypeLikelihoods.Add(await CalculateLikelihood(genotype, frequencySet, allowedLoci));
                    }
                }

                return genotypeLikelihoods.ToDictionary();
            }
        }

        private async Task<KeyValuePair<StringGenotype, decimal>> CalculateLikelihood(
            StringGenotype diplotype,
            HaplotypeFrequencySet frequencySet,
            ISet<Locus> allowedLoci)
        {
            var likelihood = await genotypeLikelihoodService.CalculateLikelihoodForDiplotype(diplotype, frequencySet, allowedLoci);
            return new KeyValuePair<StringGenotype, decimal>(diplotype, likelihood);
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