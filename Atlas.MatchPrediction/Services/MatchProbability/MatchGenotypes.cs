using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Services.MatchCalculation;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public interface IMatchGenotypes
    {
        public Task<IEnumerable<UnorderedPair<PhenotypeInfo<string>>>> PairsWithAlleleLevelMatch(
            IEnumerable<PhenotypeInfo<string>> patientGenotype,
            IEnumerable<PhenotypeInfo<string>> donorGenotype,
            string hlaNomenclatureVersion);
    }

    public class MatchGenotypes : IMatchGenotypes
    {
        private readonly IMatchCalculationService matchCalculationService;

        public MatchGenotypes(IMatchCalculationService matchCalculationService)
        {
            this.matchCalculationService = matchCalculationService;
        }

        public async Task<IEnumerable<UnorderedPair<PhenotypeInfo<string>>>> PairsWithAlleleLevelMatch(
            IEnumerable<PhenotypeInfo<string>> patientGenotype,
            IEnumerable<PhenotypeInfo<string>> donorGenotype,
            string hlaNomenclatureVersion)
        {
            var allPatientDonorCombinations = patientGenotype.SelectMany(patientHla =>
                donorGenotype.Select(donorHla => new UnorderedPair<PhenotypeInfo<string>> {Item1 = patientHla, Item2 = donorHla})).ToList();

            var pairsWithAlleleLevelMatch = new List<UnorderedPair<PhenotypeInfo<string>>>();

            foreach (var patientDonorPair in allPatientDonorCombinations)
            {
                var matchCounts = await matchCalculationService.MatchAtPGroupLevel(
                    patientDonorPair.Item1,
                    patientDonorPair.Item2,
                    hlaNomenclatureVersion);

                if (matchCounts.Reduce((locus, value, accumulator) => accumulator + value ?? accumulator, 0) == 10)
                {
                    pairsWithAlleleLevelMatch.Add(patientDonorPair);
                }
            }

            return pairsWithAlleleLevelMatch;
        }
    }
}
