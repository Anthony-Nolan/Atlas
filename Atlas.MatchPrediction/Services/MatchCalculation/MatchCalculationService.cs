using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Matching.Services;
using Atlas.MatchPrediction.Client.Models.MatchCalculation;

namespace Atlas.MatchPrediction.Services.MatchCalculation
{
    public interface IMatchCalculationService
    {
        public Task<MatchCalculationResponse> MatchAtPGroupLevel(PhenotypeInfo<string> patientGenotype, PhenotypeInfo<string> donorGenotype);
    }

    public class MatchCalculationService : IMatchCalculationService
    {
        private readonly ILocusMatchCalculator locusMatchCalculator;

        public MatchCalculationService(ILocusMatchCalculator locusMatchCalculator)
        {
            this.locusMatchCalculator = locusMatchCalculator;
        }

        public async Task<MatchCalculationResponse> MatchAtPGroupLevel(PhenotypeInfo<string> patientGenotype, PhenotypeInfo<string> donorGenotype)
        {
            // TODO: ATLAS-217/ATLAS-415: convert genotype to PGroup

            // TODO: ATLAS-217/ATLAS-417: Return MatchHla & 10/10 result

            return new MatchCalculationResponse();
        }
    }
}
