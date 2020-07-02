using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.MatchCalculation;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    internal interface IGenotypeMatcher
    {
        /// <summary>
        /// Returns all possible combinations of donor and patient genotypes with their matchCounts at the PGroup level
        /// </summary>
        /// <param name="patientGenotypes">List of unambiguous genotypes for a given patient</param>
        /// <param name="donorGenotypes">List of unambiguous genotypes for a given donor</param>
        /// <param name="hlaNomenclatureVersion">Same version used to get the genotype information</param>
        public Task<ISet<GenotypeMatchDetails>> PairsWithMatch(
            ISet<PhenotypeInfo<string>> patientGenotypes,
            ISet<PhenotypeInfo<string>> donorGenotypes,
            string hlaNomenclatureVersion);
    }

    internal class GenotypeMatcher : IGenotypeMatcher
    {
        private readonly IMatchCalculationService matchCalculationService;

        public GenotypeMatcher(IMatchCalculationService matchCalculationService)
        {
            this.matchCalculationService = matchCalculationService;
        }

        public async Task<ISet<GenotypeMatchDetails>> PairsWithMatch(
            ISet<PhenotypeInfo<string>> patientGenotypes,
            ISet<PhenotypeInfo<string>> donorGenotypes,
            string hlaNomenclatureVersion)
        {
            var allPatientDonorCombinations = patientGenotypes.SelectMany(patientHla =>
                donorGenotypes.Select(donorHla => new Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>>(patientHla, donorHla)));

            var patientDonorMatchDetails =
                await Task.WhenAll(allPatientDonorCombinations.Select(pd => CalculateMatch(pd, hlaNomenclatureVersion)));

            return patientDonorMatchDetails.ToHashSet();
        }

        private async Task<GenotypeMatchDetails> CalculateMatch(
            Tuple<PhenotypeInfo<string>, PhenotypeInfo<string>> patientDonorPair,
            string hlaNomenclatureVersion)
        {
            return await matchCalculationService.MatchAtPGroupLevel(patientDonorPair.Item1, patientDonorPair.Item2, hlaNomenclatureVersion);
        }
    }
}
