using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using AutoMapper;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring
{
    public interface IScoringRequestService
    {
        Task<ScoringResult> Score(ScoringRequest<PhenotypeInfo<string>> scoringRequest);
    }

    public class ScoringRequestService : IScoringRequestService
    {
        private readonly IDonorScoringService donorScoringService;
        private readonly IMapper mapper;

        public ScoringRequestService(IDonorScoringService donorScoringService, IMapper mapper)
        {
            this.donorScoringService = donorScoringService;
            this.mapper = mapper;
        }

        public async Task<ScoringResult> Score(ScoringRequest<PhenotypeInfo<string>> scoringRequest)
        {
            var scoringResult = await donorScoringService.ScoreDonorHlaAgainstPatientHla(scoringRequest);

            return mapper.Map<ScoringResult>(scoringResult);
        }
    }
}