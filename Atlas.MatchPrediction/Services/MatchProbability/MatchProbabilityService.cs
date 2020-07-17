using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchCalculation;

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

        public MatchProbabilityService(
            ICompressedPhenotypeExpander compressedPhenotypeExpander,
            IGenotypeLikelihoodService genotypeLikelihoodService,
            IMatchCalculationService matchCalculationService,
            IMatchProbabilityCalculator matchProbabilityCalculator,
            IHaplotypeFrequencyService haplotypeFrequencyService,
            ILogger logger)
        {
            this.compressedPhenotypeExpander = compressedPhenotypeExpander;
            this.genotypeLikelihoodService = genotypeLikelihoodService;
            this.matchCalculationService = matchCalculationService;
            this.matchProbabilityCalculator = matchProbabilityCalculator;
            this.haplotypeFrequencyService = haplotypeFrequencyService;
            this.logger = logger;
        }

        public async Task<MatchProbabilityResponse> CalculateMatchProbability(MatchProbabilityInput matchProbabilityInput)
        {
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
            return await logger.RunTimedAsync(
                async () => await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                    phenotype,
                    hlaNomenclatureVersion,
                    allowedLoci,
                    haplotypeFrequencies.Keys
                ),
                $"{LoggingPrefix}Expanded {subjectLogDescription} phenotype",
                LogLevel.Verbose
            );
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
            return await logger.RunTimedAsync(async () =>
                   {
                       var genotypeMatchingTasks = allPatientDonorCombinations
                           .Select(pd => CalculateMatch(pd, matchProbabilityInput.HlaNomenclatureVersion, allowedLoci))
                           .ToList();
                       return (await Task.WhenAll(genotypeMatchingTasks)).ToHashSet();
                   },
                $"{LoggingPrefix}Calculated genotype matches",
                LogLevel.Verbose
            );
        }

        private async Task<Dictionary<PhenotypeInfo<string>, decimal>> CalculateGenotypeLikelihoods(
            ISet<PhenotypeInfo<string>> genotypes,
            HaplotypeFrequencySet frequencySet,
            ISet<Locus> allowedLoci)
        {
            return (await logger.RunTimedAsync(async () =>
                    {
                        var genotypeLikelihoodTasks = genotypes.Select(genotype => CalculateLikelihood(genotype, frequencySet, allowedLoci));
                        return await Task.WhenAll(genotypeLikelihoodTasks);
                    },
                    $"{LoggingPrefix}Calculated likelihoods for genotypes",
                    LogLevel.Verbose
                ))
                .ToDictionary();
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