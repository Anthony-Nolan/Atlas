using System;
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
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchCalculation;

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
        // TODO: ATLAS-590: This constructor will need to be able to handle G/P group typed genotypes.
        public static async Task<GenotypeAtDesiredResolutions> FromHaplotypeResolutions(
            PhenotypeInfo<string> haplotypeResolutions,
            IHlaMetadataDictionary hlaMetadataDictionary)
        {
            return new GenotypeAtDesiredResolutions(
                haplotypeResolutions,
                await haplotypeResolutions.MapAsync(async (locus, _, hla) =>
                    hla == null ? null : await hlaMetadataDictionary.ConvertGGroupToPGroup(locus, hla)
                )
            );
        }

        public GenotypeAtDesiredResolutions(PhenotypeInfo<string> haplotypeResolution, PhenotypeInfo<string> stringMatchableResolution)
        {
            HaplotypeResolution = haplotypeResolution;
            StringMatchableResolution = stringMatchableResolution;
        }

        /// <summary>
        /// HLA at the resolution at which they were stored.
        /// TODO: ATLAS-590: The below comment will not be true until ATLAS-590 is complete.
        /// i.e. G group, or P group if any null alleles are present in the haplotype.
        /// </summary>
        public PhenotypeInfo<string> HaplotypeResolution { get; }

        /// <summary>
        /// HLA at a resolution at which it is possible to calculate match counts using string comparison only, no expansion.
        /// TODO: ATLAS-572: Ensure the null homozygous case is covered.
        /// i.e. P group, or G group for null expressing alleles. 
        /// </summary>
        public PhenotypeInfo<string> StringMatchableResolution { get; }
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
            matchPredictionLoggingContext.Initialise(matchProbabilityInput);

            var allowedLoci = GetAllowedLoci(matchProbabilityInput);
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

            //TODO: ATLAS-566 : Currently for patient/donor pairs the threshold is about one million before the request starts taking  >2 minutes
            if (donorGenotypes.Count * patientGenotypes.Count > 1_000_000)
            {
                throw new NotImplementedException(
                    "Calculating the MatchCounts of provided donor patient pairs would take upwards of 2 minutes." +
                    " This code path is not currently supported for such a large data set."
                );
            }

            var hlaMetadataDictionary = hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersion);

            async Task<List<GenotypeAtDesiredResolutions>> ConvertGenotypes(ISet<PhenotypeInfo<string>> genotypes, string subjectLogDescription)
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

            var patientDonorMatchDetails = CalculatePairsMatchCounts(allPatientDonorCombinations, allowedLoci);

            // TODO: ATLAS-233: Re-introduce hardcoded 100% probability for guaranteed match but no represented genotypes
            var patientGenotypeLikelihoods = await CalculateGenotypeLikelihoods(patientGenotypes, frequencySets.PatientSet, allowedLoci);
            var donorGenotypeLikelihoods = await CalculateGenotypeLikelihoods(donorGenotypes, frequencySets.DonorSet, allowedLoci);

            using (logger.RunTimed("Calculate match probability", LogLevel.Verbose))
            {
                return matchProbabilityCalculator.CalculateMatchProbability(
                    new SubjectCalculatorInputs {Genotypes = patientGenotypes, GenotypeLikelihoods = patientGenotypeLikelihoods},
                    new SubjectCalculatorInputs {Genotypes = donorGenotypes, GenotypeLikelihoods = donorGenotypeLikelihoods},
                    patientDonorMatchDetails,
                    allowedLoci);
            }
        }

        private async Task<ISet<PhenotypeInfo<string>>> ExpandToGenotypes(
            PhenotypeInfo<string> phenotype,
            int frequencySetId,
            ISet<Locus> allowedLoci,
            string hlaNomenclatureVersion,
            string subjectLogDescription = null)
        {
            var haplotypeFrequencies = await haplotypeFrequencyService.GetAllHaplotypeFrequencies(frequencySetId);
            using (logger.RunTimed($"{LoggingPrefix}Expand {subjectLogDescription} phenotype", LogLevel.Verbose))
            {
                return await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                    phenotype,
                    hlaNomenclatureVersion,
                    allowedLoci,
                    haplotypeFrequencies.Keys
                );
            }
        }

        private static ISet<Locus> GetAllowedLoci(MatchProbabilityInput matchProbabilityInput)
        {
            return matchProbabilityInput.PatientHla.Reduce((locus, value, accumulator) =>
            {
                if (value.Position1And2Null() || matchProbabilityInput.DonorHla.GetLocus(locus).Position1And2Null())
                {
                    accumulator.Remove(locus);
                }

                return accumulator;
            }, LocusSettings.MatchPredictionLoci);
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

                logger.SendTrace($"Patient/donor pairs: {combinations.Count}", LogLevel.Verbose);

                return combinations;
            }
        }

        private ISet<GenotypeMatchDetails> CalculatePairsMatchCounts(
            IEnumerable<Tuple<GenotypeAtDesiredResolutions, GenotypeAtDesiredResolutions>> allPatientDonorCombinations,
            ISet<Locus> allowedLoci)
        {
            using (logger.RunTimed($"{LoggingPrefix}Calculate genotype matches", LogLevel.Verbose))
            {
                return allPatientDonorCombinations
                    .AsParallel()
                    .Select(pd =>
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
                    })
                    .ToHashSet();
            }
        }

        private async Task<Dictionary<PhenotypeInfo<string>, decimal>> CalculateGenotypeLikelihoods(
            ISet<PhenotypeInfo<string>> genotypes,
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

        private async Task<KeyValuePair<PhenotypeInfo<string>, decimal>> CalculateLikelihood(
            PhenotypeInfo<string> genotype,
            HaplotypeFrequencySet frequencySet,
            ISet<Locus> allowedLoci)
        {
            var likelihood = await genotypeLikelihoodService.CalculateLikelihood(genotype, frequencySet, allowedLoci);
            return new KeyValuePair<PhenotypeInfo<string>, decimal>(genotype, likelihood);
        }
    }
}