using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Services.MatchCalculation;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public interface IGenotypeMatcher
    {
        /// <summary>
        /// For a list of patient and donor genotypes returning a list of donor patient pairs with 10/10 match at the PGroup level.
        /// </summary>
        /// <param name="patientGenotypes">List of unambiguous genotypes for a given patient</param>
        /// <param name="donorGenotypes">List of unambiguous genotypes for a given donor</param>
        /// <param name="hlaNomenclatureVersion">Same version used to get the genotype information</param>
        public Task<IEnumerable<UnorderedPair<PhenotypeInfo<string>>>> PairsWithTenOutOfTenMatch(
            IEnumerable<PhenotypeInfo<string>> patientGenotypes,
            IEnumerable<PhenotypeInfo<string>> donorGenotypes,
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
            IEnumerable<PhenotypeInfo<string>> patientGenotypes,
            IEnumerable<PhenotypeInfo<string>> donorGenotypes,
            string hlaNomenclatureVersion)
        {
            var allPatientDonorCombinations = patientGenotypes.SelectMany(patientHla =>
                donorGenotypes.Select(donorHla => new UnorderedPair<PhenotypeInfo<string>> {Item1 = patientHla, Item2 = donorHla})).ToList();

            var pairsWithAlleleLevelMatch = new List<UnorderedPair<PhenotypeInfo<string>>>();

            foreach (var patientDonorPair in allPatientDonorCombinations)
            {
                var matchCounts = await matchCalculationService.MatchAtPGroupLevel(
                    patientDonorPair.Item1,
                    patientDonorPair.Item2,
                    hlaNomenclatureVersion);

                if (matchCounts.IsTenOutOfTenMatch)
                {
                    pairsWithAlleleLevelMatch.Add(patientDonorPair);
                }
            }

            return pairsWithAlleleLevelMatch;
        }
    }
}
