using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.MatchCalculation;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public interface IGenotypeMatcher
    {
        /// <summary>
        /// Gets all possible combinations of donor and patient genotypes and returns a list of donor patient pairs where there is 10/10 match at the PGroup level
        /// </summary>
        /// <param name="patientGenotypes">List of unambiguous genotypes for a given patient</param>
        /// <param name="donorGenotypes">List of unambiguous genotypes for a given donor</param>
        /// <param name="hlaNomenclatureVersion">Same version used to get the genotype information</param>
        public Task<IEnumerable<UnorderedPair<PhenotypeInfo<string>>>> PairsWithTenOutOfTenMatch(
            HashSet<PhenotypeInfo<string>> patientGenotypes,
            HashSet<PhenotypeInfo<string>> donorGenotypes,
            string hlaNomenclatureVersion);
    }

    public class GenotypeMatcher : IGenotypeMatcher
    {
        private readonly IMatchCalculationService matchCalculationService;

        public GenotypeMatcher(IMatchCalculationService matchCalculationService)
        {
            this.matchCalculationService = matchCalculationService;
        }

        public async Task<IEnumerable<UnorderedPair<PhenotypeInfo<string>>>> PairsWithTenOutOfTenMatch(
            HashSet<PhenotypeInfo<string>> patientGenotypes,
            HashSet<PhenotypeInfo<string>> donorGenotypes,
            string hlaNomenclatureVersion)
        {
            var allPatientDonorCombinations = patientGenotypes.SelectMany(patientHla =>
                donorGenotypes.Select(donorHla => 
                    new UnorderedPair<PhenotypeInfo<string>> {Item1 = patientHla, Item2 = donorHla})).ToList();

            var patientDonorMatchDetails =
                await Task.WhenAll(
                    allPatientDonorCombinations.Select(pd => CalculateMatch(pd, hlaNomenclatureVersion)));

            var tenOutOfTenPatientDonorMatchDetails =
                patientDonorMatchDetails.Where(pd => pd.Item2.IsTenOutOfTenMatch);

            var tenOutOfTenPatientDonorPairs =
                tenOutOfTenPatientDonorMatchDetails.Select(pd => pd.Item1);

            return tenOutOfTenPatientDonorPairs;
        }

        private async Task<(UnorderedPair<PhenotypeInfo<string>>, GenotypeMatchDetails)> CalculateMatch(
            UnorderedPair<PhenotypeInfo<string>> patientDonorPair,
            string hlaNomenclatureVersion)
        {
            var matchDetails =
                await matchCalculationService.MatchAtPGroupLevel(patientDonorPair.Item1, patientDonorPair.Item2, hlaNomenclatureVersion);
                return (patientDonorPair, matchDetails);
        }
    }
}
