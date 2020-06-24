using System.Linq;
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

        public MatchProbabilityService(ICompressedPhenotypeExpander compressedPhenotypeExpander)
        {
            this.compressedPhenotypeExpander = compressedPhenotypeExpander;
        }

        public async Task<MatchProbabilityResponse> CalculateMatchProbability(MatchProbabilityInput matchProbabilityInput)
        {
            var patientGenotypes =
                await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                    matchProbabilityInput.PatientHla,
                    matchProbabilityInput.HlaNomenclatureVersion);
            var donorGenotypes =
                await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                    matchProbabilityInput.DonorHla,
                    matchProbabilityInput.HlaNomenclatureVersion);

            return new MatchProbabilityResponse();
        }
    }
}