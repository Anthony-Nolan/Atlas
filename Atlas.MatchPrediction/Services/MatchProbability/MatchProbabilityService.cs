using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using static Atlas.Common.Maths.Combinations;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Utils;
using Atlas.MatchPrediction.Validators;
using FluentValidation;
using LoggingStopwatch;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;
using TypedGenotype = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<Atlas.MatchPrediction.ExternalInterface.Models.HlaAtKnownTypingCategory>;
using StringGenotype = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;

// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable SuggestBaseTypeForParameter

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public interface IMatchProbabilityService
    {
        public Task<MatchProbabilityResponse> CalculateMatchProbability(SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput);
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
        /// i.e. P group, or G group for null expressing alleles. 
        /// </summary>
        public StringGenotype StringMatchableResolution { get; }

        /// <summary>
        /// Likelihood of this genotype.
        ///
        /// Stored with the genotype to avoid dictionary lookups when calculating final likelihoods, as looking up the same genotype multiple times
        /// for different patient/donor pairs is inefficient 
        /// </summary>
        public decimal GenotypeLikelihood { get; }

        private GenotypeAtDesiredResolutions(TypedGenotype haplotypeResolution, StringGenotype stringMatchableResolution, decimal genotypeLikelihood)
        {
            HaplotypeResolution = haplotypeResolution.ToHlaNames();
            StringMatchableResolution = stringMatchableResolution;
            GenotypeLikelihood = genotypeLikelihood;
        }

        public static async Task<GenotypeAtDesiredResolutions> FromHaplotypeResolutions(
            TypedGenotype haplotypeResolutions,
            IHlaMetadataDictionary hlaMetadataDictionary,
            decimal genotypeLikelihood)
        {
            var stringMatchableResolutions = (await haplotypeResolutions.MapAsync(async (locus, _, hla) =>
            {
                if (hla?.Hla == null)
                {
                    return null;
                }

                return hla.TypingCategory switch
                {
                    HaplotypeTypingCategory.GGroup => await hlaMetadataDictionary.ConvertGGroupToPGroup(locus, hla.Hla),
                    HaplotypeTypingCategory.PGroup => hla.Hla,
                    HaplotypeTypingCategory.SmallGGroup => await hlaMetadataDictionary.ConvertSmallGGroupToPGroup(locus, hla.Hla),
                    _ => throw new ArgumentOutOfRangeException(nameof(hla.TypingCategory))
                };
            })).CopyExpressingAllelesToNullPositions();

            return new GenotypeAtDesiredResolutions(haplotypeResolutions, stringMatchableResolutions, genotypeLikelihood);
        }
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
            var patientHlaVersion = frequencySets.PatientSet.HlaNomenclatureVersion;
            var donorHlaVersion = frequencySets.DonorSet.HlaNomenclatureVersion;

            var patientGenotypes = await ExpandToGenotypes(
                singleDonorMatchProbabilityInput.PatientHla.ToPhenotypeInfo(),
                frequencySets.PatientSet.Id,
                allowedLoci,
                patientHlaVersion,
                "patient"
            );

            var donorGenotypes = await ExpandToGenotypes(
                singleDonorMatchProbabilityInput.Donor.DonorHla.ToPhenotypeInfo(),
                frequencySets.DonorSet.Id,
                allowedLoci,
                donorHlaVersion,
                "donor"
            );

            if (donorGenotypes.IsNullOrEmpty() || patientGenotypes.IsNullOrEmpty())
            {
                LogUnrepresentedGenotypes(donorGenotypes, patientGenotypes);
                return new MatchProbabilityResponse(null, allowedLoci)
                {
                    IsDonorPhenotypeUnrepresented = donorGenotypes.IsNullOrEmpty(),
                    IsPatientPhenotypeUnrepresented = patientGenotypes.IsNullOrEmpty(),
                    DonorHaplotypeFrequencySet = frequencySets.DonorSet.ToClientHaplotypeFrequencySet(),
                    PatientHaplotypeFrequencySet = frequencySets.PatientSet.ToClientHaplotypeFrequencySet()
                };
            }

            var patientStringGenotypes = patientGenotypes.Select(g => g.ToHlaNames()).ToHashSet();
            var donorStringGenotypes = donorGenotypes.Select(g => g.ToHlaNames()).ToHashSet();

            var patientGenotypeLikelihoods = await CalculateGenotypeLikelihoods(patientStringGenotypes, frequencySets.PatientSet, allowedLoci);
            var donorGenotypeLikelihoods = await CalculateGenotypeLikelihoods(donorStringGenotypes, frequencySets.DonorSet, allowedLoci);

            var truncatedDonor = ExpandedGenotypeTruncater.TruncateGenotypes(donorGenotypeLikelihoods, donorGenotypes);
            var truncatedPatient = ExpandedGenotypeTruncater.TruncateGenotypes(patientGenotypeLikelihoods, patientGenotypes);

            var convertedPatientGenotypes = await genotypeConverter.ConvertGenotypes(
                truncatedPatient.Genotypes, "patient", truncatedPatient.GenotypeLikelihoods, patientHlaVersion);
            var convertedDonorGenotypes = await genotypeConverter.ConvertGenotypes(
                truncatedDonor.Genotypes, "donor", truncatedDonor.GenotypeLikelihoods, donorHlaVersion);
            var allPatientDonorCombinations = CombineGenotypes(convertedPatientGenotypes, convertedDonorGenotypes);

            using (var matchCountLogger = MatchCountLogger(NumberOfPairsOfCartesianProduct(convertedDonorGenotypes, convertedPatientGenotypes)))
            {
                var patientDonorMatchDetails = CalculatePairsMatchCounts(allPatientDonorCombinations, allowedLoci, matchCountLogger);

                // Sum likelihoods outside of loop, so they are not calculated millions of times
                var sumOfPatientLikelihoods = truncatedPatient.GenotypeLikelihoods.Values.SumDecimals();
                var sumOfDonorLikelihoods = truncatedDonor.GenotypeLikelihoods.Values.SumDecimals();

                using (logger.RunTimed("Calculate match probability", LogLevel.Verbose))
                {
                    var matchProbability = matchProbabilityCalculator.CalculateMatchProbability(
                        sumOfPatientLikelihoods,
                        sumOfDonorLikelihoods,
                        patientDonorMatchDetails,
                        allowedLoci
                    );

                    matchProbability.DonorHaplotypeFrequencySet = frequencySets.DonorSet.ToClientHaplotypeFrequencySet();
                    matchProbability.PatientHaplotypeFrequencySet = frequencySets.PatientSet.ToClientHaplotypeFrequencySet();

                    return matchProbability;
                }
            }
        }

        private void LogUnrepresentedGenotypes(ISet<TypedGenotype> donorGenotypes, ISet<TypedGenotype> patientGenotypes)
        {
            if (donorGenotypes.IsNullOrEmpty())
            {
                logger.SendTrace($"{LoggingPrefix}Donor genotype unrepresented.", LogLevel.Verbose);
            }

            if (patientGenotypes.IsNullOrEmpty())
            {
                logger.SendTrace($"{LoggingPrefix}Patient genotype unrepresented.", LogLevel.Verbose);
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
                        HlaNomenclatureVersion = hlaNomenclatureVersion,
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
            ISet<StringGenotype> genotypes,
            HaplotypeFrequencySet frequencySet,
            ISet<Locus> allowedLoci)
        {
            using (logger.RunTimed($"{LoggingPrefix}Calculate likelihoods for genotypes", LogLevel.Verbose))
            {
                var genotypeLikelihoods = new List<KeyValuePair<StringGenotype, decimal>>();

                // If there is no ambiguity for an input genotype, we do not need to use haplotype frequencies to work out the likelihood of said genotype - it is already guaranteed! 
                if (genotypes.Count == 1)
                {
                    genotypeLikelihoods.Add(new KeyValuePair<StringGenotype, decimal>(genotypes.Single(), 1));
                }
                else
                {
                    foreach (var genotype in genotypes)
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
    }
}