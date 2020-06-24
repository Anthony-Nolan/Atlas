using System.Threading.Tasks;
using Atlas.MatchPrediction.Client.Models.MatchProbability;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public interface IMatchProbabilityService
    {
        public Task<MatchProbabilityResponse> CalculateMatchProbability(MatchProbabilityInput matchProbabilityInput);
    }

    public class MatchProbabilityService : IMatchProbabilityService
    {
        private readonly ICompressedPhenotypeExpander compressedPhenotypeExpander;
        private readonly IMatchGenotypes matchGenotypes;

        public MatchProbabilityService(
            ICompressedPhenotypeExpander compressedPhenotypeExpander,
            IMatchGenotypes matchGenotypes)
        {
            this.compressedPhenotypeExpander = compressedPhenotypeExpander;
            this.matchGenotypes = matchGenotypes;
        }

        public async Task<MatchProbabilityResponse> CalculateMatchProbability(MatchProbabilityInput matchProbabilityInput)
        {
            var patientGenotypes =
                await compressedPhenotypeExpander.ExpandCompressedPhenotype(matchProbabilityInput.PatientHla, matchProbabilityInput.HlaNomenclatureVersion);
            var donorGenotypes =
                await compressedPhenotypeExpander.ExpandCompressedPhenotype(matchProbabilityInput.DonorHla, matchProbabilityInput.HlaNomenclatureVersion);

            var matchingPairs = 
                await matchGenotypes.PairsWithAlleleLevelMatch(patientGenotypes, donorGenotypes, matchProbabilityInput.HlaNomenclatureVersion);

            return new MatchProbabilityResponse();
        }
    }
}