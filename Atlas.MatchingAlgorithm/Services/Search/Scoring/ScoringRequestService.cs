using AutoMapper;
using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Extensions;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;

namespace Atlas.MatchingAlgorithm.Services.Scoring
{
    public interface IScoringRequestService
    {
        Task<ScoringResult> Score(ScoringRequest scoringRequest);
    }
    
    public class ScoringRequestService: IScoringRequestService
    {
        private readonly IDonorScoringService donorScoringService;
        private readonly IMapper mapper;

        public ScoringRequestService(IDonorScoringService donorScoringService, IMapper mapper)
        {
            this.donorScoringService = donorScoringService;
            this.mapper = mapper;
        }   
        
        public async Task<ScoringResult> Score(ScoringRequest scoringRequest)
        {
            var donorHla = scoringRequest.DonorHla.ToPhenotypeInfo();
            var patientHla = scoringRequest.PatientHla.ToPhenotypeInfo();

            var scoringResult = await donorScoringService.ScoreDonorHlaAgainstPatientHla(donorHla, patientHla);

            return mapper.Map<ScoringResult>(scoringResult);
        }
    }
}