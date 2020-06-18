using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Matching.Services;
using Atlas.MatchPrediction.Client.Models.MatchCalculation;

namespace Atlas.MatchPrediction.Services.MatchCalculation
{
    public interface IMatchCalculationService
    {
        public Task<MatchCalculationResponse> MatchAtGGroupLevel(PhenotypeInfo<string> patientGenotype, PhenotypeInfo<string> donorGenotype);
    }

    public class MatchCalculationService : IMatchCalculationService
    {
        private readonly ILocusMatchCalculator locusMatchCalculator;

        public MatchCalculationService(ILocusMatchCalculator locusMatchCalculator)
        {
            this.locusMatchCalculator = locusMatchCalculator;
        }

        public async Task<MatchCalculationResponse> MatchAtGGroupLevel(PhenotypeInfo<string> patientGenotype, PhenotypeInfo<string> donorGenotype)
        {
            // TODO: ATLAS-415: convert genotype to PGroup

            // TODO: ATLAS-417: Return MatchHla & 10/10 result

            return new MatchCalculationResponse();
        }
    }
}
