using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using AutoMapper;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring
{
    public interface IScoringRequestService
    {
        Task<ScoringResult> Score(DonorHlaScoringRequest scoringRequest);
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

        public async Task<ScoringResult> Score(DonorHlaScoringRequest scoringRequest)
        {
            var scoringResult = await donorScoringService.ScoreDonorHlaAgainstPatientHla(scoringRequest);

            return mapper.Map<ScoringResult>(scoringResult);
        }
    }
}