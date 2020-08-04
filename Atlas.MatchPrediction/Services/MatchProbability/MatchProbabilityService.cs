﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Validators;
using FluentValidation;
using LoggingStopwatch;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;
using TypedGenotype = Atlas.Common.GeneticData.PhenotypeInfo.PhenotypeInfo<Atlas.MatchPrediction.ExternalInterface.Models.HlaAtKnownTypingCategory>;
using StringGenotype = Atlas.Common.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;

// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable SuggestBaseTypeForParameter

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public interface IMatchProbabilityService
    {
        public Task<MatchProbabilityResponse> CalculateMatchProbability(MatchProbabilityInput matchProbabilityInput);
    }

    internal class GenotypeAtDesiredResolutions
    {
        /// <summary>
        /// HLA at the resolution at which they were stored.
        /// i.e. G group, or P group if any null alleles are present in the haplotype.
        /// </summary>
        public StringGenotype HaplotypeResolution { get; }

        /// <summary>
        /// HLA at a resolution at which it is possible to calculate match counts using string comparison only, no expansion.
        /// TODO: ATLAS-572: Ensure the null homozygous case is covered.
        /// i.e. P group, or G group for null expressing alleles. 
        /// </summary>
        public StringGenotype StringMatchableResolution { get; }

        private GenotypeAtDesiredResolutions(TypedGenotype haplotypeResolution, StringGenotype stringMatchableResolution)
        {
            HaplotypeResolution = haplotypeResolution.ToHlaNames();
            StringMatchableResolution = stringMatchableResolution;
        }

        public static async Task<GenotypeAtDesiredResolutions> FromHaplotypeResolutions(
            TypedGenotype haplotypeResolutions,
            IHlaMetadataDictionary hlaMetadataDictionary)
        {
            var stringMatchableResolutions = await haplotypeResolutions.MapAsync(async (locus, _, hla) =>
            {
                if (hla?.Hla == null)
                {
                    return null;
                }

                return hla.TypingCategory switch
                {
                    HaplotypeTypingCategory.GGroup => await hlaMetadataDictionary.ConvertGGroupToPGroup(locus, hla.Hla) ?? hla.Hla,
                    HaplotypeTypingCategory.PGroup => hla.Hla,
                    _ => throw new ArgumentOutOfRangeException(nameof(hla.TypingCategory))
                };
            });

            return new GenotypeAtDesiredResolutions(haplotypeResolutions, stringMatchableResolutions);
        }
    }

    internal class MatchProbabilityService : IMatchProbabilityService
    {
        private const string LoggingPrefix = "MatchPrediction: ";

        private readonly ICompressedPhenotypeExpander compressedPhenotypeExpander;
        private readonly IGenotypeLikelihoodService genotypeLikelihoodService;
        private readonly IMatchCalculationService matchCalculationService;
        private readonly IMatchProbabilityCalculator matchProbabilityCalculator;
        private readonly IHaplotypeFrequencyService haplotypeFrequencyService;
        private readonly ILogger logger;
        private readonly MatchPredictionLoggingContext matchPredictionLoggingContext;
        private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;

        public MatchProbabilityService(
            ICompressedPhenotypeExpander compressedPhenotypeExpander,
            IGenotypeLikelihoodService genotypeLikelihoodService,
            IMatchCalculationService matchCalculationService,
            IMatchProbabilityCalculator matchProbabilityCalculator,
            IHaplotypeFrequencyService haplotypeFrequencyService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchPredictionLogger logger,
            MatchPredictionLoggingContext matchPredictionLoggingContext,
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory)
        {
            this.compressedPhenotypeExpander = compressedPhenotypeExpander;
            this.genotypeLikelihoodService = genotypeLikelihoodService;
            this.matchCalculationService = matchCalculationService;
            this.matchProbabilityCalculator = matchProbabilityCalculator;
            this.haplotypeFrequencyService = haplotypeFrequencyService;
            this.logger = logger;
            this.matchPredictionLoggingContext = matchPredictionLoggingContext;
            this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
        }

        public async Task<MatchProbabilityResponse> CalculateMatchProbability(MatchProbabilityInput matchProbabilityInput)
        {
            await new MatchProbabilityInputValidator().ValidateAndThrowAsync(matchProbabilityInput);
            
            matchPredictionLoggingContext.Initialise(matchProbabilityInput);

            var allowedLoci = LocusSettings.MatchPredictionLoci.Except(matchProbabilityInput.ExcludedLoci).ToHashSet();
            var hlaNomenclatureVersion = matchProbabilityInput.HlaNomenclatureVersion;

            var frequencySets = await haplotypeFrequencyService.GetHaplotypeFrequencySets(
                matchProbabilityInput.DonorFrequencySetMetadata,
                matchProbabilityInput.PatientFrequencySetMetadata
            );

            var patientGenotypes = await ExpandToGenotypes(
                matchProbabilityInput.PatientHla,
                frequencySets.PatientSet.Id,
                allowedLoci,
                hlaNomenclatureVersion,
                "patient"
            );

            var donorGenotypes = await ExpandToGenotypes(
                matchProbabilityInput.DonorHla,
                frequencySets.DonorSet.Id,
                allowedLoci,
                hlaNomenclatureVersion,
                "donor"
            );

            if (donorGenotypes.IsNullOrEmpty() || patientGenotypes.IsNullOrEmpty())
            {
                if (donorGenotypes.IsNullOrEmpty())
                {
                    logger.SendTrace($"{LoggingPrefix}Donor genotype unrepresented.", LogLevel.Verbose);
                }

                if (patientGenotypes.IsNullOrEmpty())
                {
                    logger.SendTrace($"{LoggingPrefix}Patient genotype unrepresented.", LogLevel.Verbose);
                }

                return new MatchProbabilityResponse(null, allowedLoci)
                {
                    IsDonorPhenotypeUnrepresented = donorGenotypes.IsNullOrEmpty(),
                    IsPatientPhenotypeUnrepresented = patientGenotypes.IsNullOrEmpty()
                };
            }

            // TODO: ATLAS-566: Currently for patient/donor pairs the threshold is about twenty million before the request starts taking >2 minutes
            if (donorGenotypes.Count * patientGenotypes.Count > 20_000_000)
            {
                throw new NotImplementedException(
                    "Calculating the MatchCounts of provided donor patient pairs would take upwards of 2 minutes." +
                    " This code path is not currently supported for such a large data set." +
                    $"[{donorGenotypes.Count * patientGenotypes.Count} pairs to calculate.]"
                );
            }

            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);

            async Task<List<GenotypeAtDesiredResolutions>> ConvertGenotypes(
                ISet<TypedGenotype> genotypes,
                string subjectLogDescription)
            {
                using (logger.RunTimed($"Convert genotypes for matching: {subjectLogDescription}", LogLevel.Verbose))
                {
                    return (await Task.WhenAll(
                            genotypes.Select(async g => await GenotypeAtDesiredResolutions.FromHaplotypeResolutions(g, hlaMetadataDictionary)))
                        ).ToList();
                }
            }

            var allPatientDonorCombinations = CombineGenotypes(
                await ConvertGenotypes(patientGenotypes, "patient"),
                await ConvertGenotypes(donorGenotypes, "donor"));


            var patientStringGenotypes = patientGenotypes.Select(g => g.ToHlaNames()).ToHashSet();
            var donorStringGenotypes = donorGenotypes.Select(g => g.ToHlaNames()).ToHashSet();

            // TODO: ATLAS-233: Re-introduce hardcoded 100% probability for guaranteed match but no represented genotypes
            var patientGenotypeLikelihoods = await CalculateGenotypeLikelihoods(patientStringGenotypes, frequencySets.PatientSet, allowedLoci);
            var donorGenotypeLikelihoods = await CalculateGenotypeLikelihoods(donorStringGenotypes, frequencySets.DonorSet, allowedLoci);

            using (var matchCountLogger = MatchCountLogger(allPatientDonorCombinations.Count))
            {
                var patientDonorMatchDetails = CalculatePairsMatchCounts(allPatientDonorCombinations, allowedLoci, matchCountLogger);
                using (logger.RunTimed("Calculate match probability", LogLevel.Verbose))
                {
                    return matchProbabilityCalculator.CalculateMatchProbability(
                        new SubjectCalculatorInputs {Genotypes = patientStringGenotypes, GenotypeLikelihoods = patientGenotypeLikelihoods},
                        new SubjectCalculatorInputs {Genotypes = donorStringGenotypes, GenotypeLikelihoods = donorGenotypeLikelihoods},
                        patientDonorMatchDetails,
                        allowedLoci);
                }
            }
        }

        private async Task<ISet<TypedGenotype>> ExpandToGenotypes(
            StringGenotype phenotype,
            int frequencySetId,
            ISet<Locus> allowedLoci,
            string hlaNomenclatureVersion,
            string subjectLogDescription = null)
        {
            var haplotypeFrequencies = await haplotypeFrequencyService.GetAllHaplotypeFrequencies(frequencySetId);
            using (logger.RunTimed($"{LoggingPrefix}Expand {subjectLogDescription} phenotype", LogLevel.Verbose))
            {
                return await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                    new ExpandCompressedPhenotypeInput
                    {
                        Phenotype = phenotype,
                        AllowedLoci = allowedLoci,
                        HlaNomenclatureVersion = hlaNomenclatureVersion,
                        AllHaplotypes = new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
                        {
                            GGroup = haplotypeFrequencies
                                .Where(h => h.Value.TypingCategory == HaplotypeTypingCategory.GGroup)
                                .Select(h => h.Value.Hla)
                                .ToList(),
                            PGroup = haplotypeFrequencies
                                .Where(h => h.Value.TypingCategory == HaplotypeTypingCategory.PGroup)
                                .Select(h => h.Value.Hla)
                                .ToList()
                        }
                    }
                );
            }
        }

        private List<Tuple<GenotypeAtDesiredResolutions, GenotypeAtDesiredResolutions>> CombineGenotypes(
            List<GenotypeAtDesiredResolutions> patientGenotypes,
            List<GenotypeAtDesiredResolutions> donorGenotypes)
        {
            using (logger.RunTimed("Combining patient/donor genotypes", LogLevel.Verbose))
            {
                var combinations = patientGenotypes.SelectMany(patientHla =>
                        donorGenotypes.Select(donorHla =>
                            new Tuple<GenotypeAtDesiredResolutions, GenotypeAtDesiredResolutions>(patientHla, donorHla)))
                    .ToList();

                logger.SendTrace($"Patient/donor pairs: {combinations.Count:n0}", LogLevel.Info);

                return combinations;
            }
        }

        private ILongOperationLoggingStopwatch MatchCountLogger(int patientDonorPairCount) => logger.RunLongOperationWithTimer(
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
                .AsParallel()
                .Select(pd =>
                {
                    using (stopwatch.TimeInnerOperation())
                    {
                        var (patient, donor) = pd;
                        return new GenotypeMatchDetails
                        {
                            AvailableLoci = allowedLoci,
                            DonorGenotype = donor.HaplotypeResolution,
                            PatientGenotype = patient.HaplotypeResolution,
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
            ISet<StringGenotype> genotypes,
            HaplotypeFrequencySet frequencySet,
            ISet<Locus> allowedLoci)
        {
            using (logger.RunTimed($"{LoggingPrefix}Calculate likelihoods for genotypes", LogLevel.Verbose))
            {
                var genotypeLikelihoodTasks = genotypes.Select(genotype => CalculateLikelihood(genotype, frequencySet, allowedLoci)).ToList();
                var genotypeLikelihoods = await Task.WhenAll(genotypeLikelihoodTasks);
                return genotypeLikelihoods.ToDictionary();
            }
        }

        private async Task<KeyValuePair<StringGenotype, decimal>> CalculateLikelihood(
            StringGenotype genotype,
            HaplotypeFrequencySet frequencySet,
            ISet<Locus> allowedLoci)
        {
            var likelihood = await genotypeLikelihoodService.CalculateLikelihood(genotype, frequencySet, allowedLoci);
            return new KeyValuePair<StringGenotype, decimal>(genotype, likelihood);
        }
    }
}