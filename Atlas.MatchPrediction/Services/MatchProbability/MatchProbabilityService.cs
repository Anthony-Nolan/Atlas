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
using HaplotypeHla = Atlas.Common.GeneticData.PhenotypeInfo.LociInfo<string>;

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
            // var patientGenotypes = await ExpandPatientPhenotype(matchProbabilityInput);
            // var donorGenotypes = await ExpandDonorPhenotype(matchProbabilityInput);

            var allowedPatientLoci = GetAllowedLoci(matchProbabilityInput.PatientHla);
            var allowedDonorLoci = GetAllowedLoci(matchProbabilityInput.DonorHla);

            var frequencySets = await haplotypeFrequencyService.GetHaplotypeFrequencySets(
                matchProbabilityInput.DonorFrequencySetMetadata,
                matchProbabilityInput.PatientFrequencySetMetadata
            );

            var donorSet = await haplotypeFrequencyService.GetAllHaplotypeFrequencies(frequencySets.DonorSet.Id);
            var patientSet = await haplotypeFrequencyService.GetAllHaplotypeFrequencies(frequencySets.PatientSet.Id);

            var donorGenotypes = await ExpandDonorPhenotypeNew(matchProbabilityInput, donorSet);
            var patientGenotypes = await ExpandPatientPhenotypeNew(matchProbabilityInput, patientSet);

            var allPatientDonorCombinations = patientGenotypes.SelectMany(patientHla =>
                    donorGenotypes.Select(donorHla => new Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>(patientHla, donorHla)))
                .ToList();

            logger.SendTrace($"Patient/donor pairs: {allPatientDonorCombinations.Count}", LogLevel.Verbose);

            var patientDonorMatchDetails = await CalculatePairsMatchCounts(matchProbabilityInput, allPatientDonorCombinations);

            // TODO: ATLAS-233: Re-introduce hardcoded 100% probability for guaranteed match but no represented genotypes

            var patientGenotypeLikelihoods = await CalculateGenotypeLikelihoods(patientGenotypes, frequencySets.PatientSet);
            var donorGenotypeLikelihoods = await CalculateGenotypeLikelihoods(donorGenotypes, frequencySets.DonorSet);

            return matchProbabilityCalculator.CalculateMatchProbability(
                new SubjectCalculatorInputs {Genotypes = patientGenotypes, GenotypeLikelihoods = patientGenotypeLikelihoods},
                new SubjectCalculatorInputs {Genotypes = donorGenotypes, GenotypeLikelihoods = donorGenotypeLikelihoods},
                patientDonorMatchDetails
            );
        }

        private static List<Locus> GetAllowedLoci(PhenotypeInfo<string> hla)
        {
            return hla.Reduce((locus, value, accumulator) =>
            {
                if (value.Position1 == null && value.Position1 == null)
                {
                    accumulator.Remove(locus);
                }

                return accumulator;
            }, LocusSettings.MatchPredictionLoci.ToList());
        }

        private async Task<ISet<PhenotypeInfo<string>>> ExpandPatientPhenotype(MatchProbabilityInput matchProbabilityInput)
        {
            return await ExpandPhenotype(matchProbabilityInput.DonorHla, matchProbabilityInput.HlaNomenclatureVersion, "patient");
        }

        private async Task<ISet<PhenotypeInfo<string>>> ExpandDonorPhenotype(MatchProbabilityInput matchProbabilityInput)
        {
            return await ExpandPhenotype(matchProbabilityInput.PatientHla, matchProbabilityInput.HlaNomenclatureVersion, "donor");
        }

        private async Task<ISet<PhenotypeInfo<string>>> ExpandPhenotype(
            PhenotypeInfo<string> hla,
            string hlaNomenclatureVersion,
            string phenotypeLogDescription)
        {
            return await logger.RunTimedAsync(
                async () => await compressedPhenotypeExpander.ExpandCompressedPhenotype(hla, hlaNomenclatureVersion),
                $"{LoggingPrefix}Expanded {phenotypeLogDescription} phenotype",
                LogLevel.Verbose
            );
        }

        // TODO: ATLAS-400: Clean up "new" vs "old" methods here
        private async Task<ISet<PhenotypeInfo<string>>> ExpandPatientPhenotypeNew(
            MatchProbabilityInput matchProbabilityInput,
            Dictionary<HaplotypeHla, decimal> haplotypeFrequencies)
        {
            return await ExpandPhenotypeNew(
                matchProbabilityInput.PatientHla,
                matchProbabilityInput.HlaNomenclatureVersion,
                "patient",
                haplotypeFrequencies
            );
        }

        private async Task<ISet<PhenotypeInfo<string>>> ExpandDonorPhenotypeNew(
            MatchProbabilityInput matchProbabilityInput,
            Dictionary<HaplotypeHla, decimal> haplotypeFrequencies)
        {
            return await ExpandPhenotypeNew(
                matchProbabilityInput.DonorHla,
                matchProbabilityInput.HlaNomenclatureVersion,
                "donor",
                haplotypeFrequencies
            );
        }

        private async Task<ISet<PhenotypeInfo<string>>> ExpandPhenotypeNew(
            PhenotypeInfo<string> hla,
            string hlaNomenclatureVersion,
            string phenotypeLogDescription,
            Dictionary<HaplotypeHla, decimal> haplotypeFrequencies)
        {
            return await logger.RunTimedAsync(
                async () => await compressedPhenotypeExpander.ExpandCompressedPhenotype(hla, hlaNomenclatureVersion, haplotypeFrequencies.Keys),
                $"{LoggingPrefix}Expanded {phenotypeLogDescription} phenotype",
                LogLevel.Verbose
            );
        }

        private async Task<ISet<GenotypeMatchDetails>> CalculatePairsMatchCounts(
            MatchProbabilityInput matchProbabilityInput,
            IEnumerable<Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>> allPatientDonorCombinations)
        {
            return await logger.RunTimedAsync(
                async () => (await Task.WhenAll(allPatientDonorCombinations
                    .Select(pd => CalculateMatch(pd, matchProbabilityInput.HlaNomenclatureVersion)))).ToHashSet(),
                $"{LoggingPrefix}Calculated genotype matches",
                LogLevel.Verbose
            );
        }

        private async Task<Dictionary<PhenotypeInfo<string>, decimal>> CalculateGenotypeLikelihoods(
            ISet<PhenotypeInfo<string>> genotypes,
            HaplotypeFrequencySet frequencySet)
        {
            return (await logger.RunTimedAsync(
                    async () => await Task.WhenAll(genotypes.Select(genotype => CalculateLikelihood(genotype, frequencySet))),
                    $"{LoggingPrefix}Calculated likelihoods for genotypes",
                    LogLevel.Verbose
                ))
                .ToDictionary();
        }

        private async Task<GenotypeMatchDetails> CalculateMatch(
            Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>> patientDonorPair,
            string hlaNomenclatureVersion)
        {
            return await matchCalculationService.MatchAtPGroupLevel(patientDonorPair.Item1, patientDonorPair.Item2,
                hlaNomenclatureVersion);
        }

        private async Task<KeyValuePair<PhenotypeInfo<string>, decimal>> CalculateLikelihood(
            PhenotypeInfo<string> genotype,
            HaplotypeFrequencySet frequencySet)
        {
            var likelihood = await genotypeLikelihoodService.CalculateLikelihood(genotype, frequencySet);
            return new KeyValuePair<PhenotypeInfo<string>, decimal>(genotype, likelihood);
        }
    }
}