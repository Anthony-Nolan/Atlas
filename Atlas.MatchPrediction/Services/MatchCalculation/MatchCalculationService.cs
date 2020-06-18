using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Matching.Services;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.Client.Models.MatchCalculation;
using Atlas.MatchPrediction.Services.Utility;

namespace Atlas.MatchPrediction.Services.MatchCalculation
{
    public interface IMatchCalculationService
    {
        public Task<MatchCalculationResponse> MatchAtPGroupLevel(
            PhenotypeInfo<string> patientGenotype,
            PhenotypeInfo<string> donorGenotype,
            string hlaNomenclatureVersion);
    }

    public class MatchCalculationService : IMatchCalculationService
    {
        private const TargetHlaCategory FrequencyResolution = TargetHlaCategory.PGroup;

        private readonly IHlaPerLocusExpander hlaPerLocusExpander;
        private readonly ILocusMatchCalculator locusMatchCalculator;

        public MatchCalculationService(
            IHlaPerLocusExpander hlaPerLocusExpander,
            ILocusMatchCalculator locusMatchCalculator)
        {
            this.hlaPerLocusExpander = hlaPerLocusExpander;
            this.locusMatchCalculator = locusMatchCalculator;
        }

        public async Task<MatchCalculationResponse> MatchAtPGroupLevel(
            PhenotypeInfo<string> patientGenotype,
            PhenotypeInfo<string> donorGenotype,
            string hlaNomenclatureVersion)
        {
            var patientGenotypeWithPGroups =
                await hlaPerLocusExpander.Expand(patientGenotype, FrequencyResolution, hlaNomenclatureVersion);
            var donorGenotypeWithPGroups =
                await hlaPerLocusExpander.Expand(patientGenotype, FrequencyResolution, hlaNomenclatureVersion);

            // TODO: ATLAS-217/ATLAS-417: Return MatchHla & 10/10 result

            return new MatchCalculationResponse();
        }
    }
}