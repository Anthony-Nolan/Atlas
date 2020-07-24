using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.Common.Utils.Models;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Microsoft.Azure.Documents.SystemFunctions;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public interface IMatchProbabilityService
    {
        public Task<MatchProbabilityResponse> CalculateMatchProbability(MatchProbabilityInput matchProbabilityInput);
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

        public MatchProbabilityService(
            ICompressedPhenotypeExpander compressedPhenotypeExpander,
            IGenotypeLikelihoodService genotypeLikelihoodService,
            IMatchCalculationService matchCalculationService,
            IMatchProbabilityCalculator matchProbabilityCalculator,
            IHaplotypeFrequencyService haplotypeFrequencyService,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchPredictionLogger logger,
            MatchPredictionLoggingContext matchPredictionLoggingContext)
        {
            this.compressedPhenotypeExpander = compressedPhenotypeExpander;
            this.genotypeLikelihoodService = genotypeLikelihoodService;
            this.matchCalculationService = matchCalculationService;
            this.matchProbabilityCalculator = matchProbabilityCalculator;
            this.haplotypeFrequencyService = haplotypeFrequencyService;
            this.logger = logger;
            this.matchPredictionLoggingContext = matchPredictionLoggingContext;
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

            var donorGenotypes = await ExpandToGenotypes(
                matchProbabilityInput.DonorHla,
                frequencySets.DonorSet.Id,
                allowedLoci,
                hlaNomenclatureVersion,
                "donor"
            );

            var patientGenotypes = await ExpandToGenotypes(
                matchProbabilityInput.PatientHla,
                frequencySets.PatientSet.Id,
                allowedLoci,
                hlaNomenclatureVersion,
                "patient"
            );

            if (donorGenotypes.IsNullOrEmpty() || patientGenotypes.IsNullOrEmpty())
            {
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

            var allPatientDonorCombinations = patientGenotypes.SelectMany(patientHla =>
                    donorGenotypes.Select(donorHla => new Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>(patientHla, donorHla)))
                .ToList();

            logger.SendTrace($"Patient/donor pairs: {allPatientDonorCombinations.Count}", LogLevel.Verbose);

            var patientDonorMatchDetails = await CalculatePairsMatchCounts(
                matchProbabilityInput,
                allPatientDonorCombinations,
                allowedLoci
            );

            // TODO: ATLAS-233: Re-introduce hardcoded 100% probability for guaranteed match but no represented genotypes
            var patientGenotypeLikelihoods = await CalculateGenotypeLikelihoods(patientGenotypes, frequencySets.PatientSet, allowedLoci);
            var donorGenotypeLikelihoods = await CalculateGenotypeLikelihoods(donorGenotypes, frequencySets.DonorSet, allowedLoci);

            return matchProbabilityCalculator.CalculateMatchProbability(
                new SubjectCalculatorInputs {Genotypes = patientGenotypes, GenotypeLikelihoods = patientGenotypeLikelihoods},
                new SubjectCalculatorInputs {Genotypes = donorGenotypes, GenotypeLikelihoods = donorGenotypeLikelihoods},
                patientDonorMatchDetails,
                allowedLoci);
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
            }, LocusSettings.MatchPredictionLoci.ToHashSet());
        }

        private async Task<ISet<GenotypeMatchDetails>> CalculatePairsMatchCounts(
            MatchProbabilityInput matchProbabilityInput,
            IEnumerable<Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>> allPatientDonorCombinations,
            ISet<Locus> allowedLoci)
        {
            using (logger.RunTimed($"{LoggingPrefix}Calculate genotype matches", LogLevel.Verbose))
            {
                var genotypeMatchingTasks = allPatientDonorCombinations
                    .Select(pd => CalculateMatch(pd, matchProbabilityInput.HlaNomenclatureVersion, allowedLoci))
                    .ToList();
                return (await Task.WhenAll(genotypeMatchingTasks)).ToHashSet();
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

        private async Task<GenotypeMatchDetails> CalculateMatch(
            Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>> patientDonorPair,
            string hlaNomenclatureVersion,
            ISet<Locus> allowedLoci)
        {
            return await matchCalculationService.MatchAtPGroupLevel(
                patientDonorPair.Item1,
                patientDonorPair.Item2,
                hlaNomenclatureVersion,
                allowedLoci);
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