using System.Threading.Tasks;
using AutoMapper;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.Extensions;

namespace Nova.SearchAlgorithm.Services.Scoring
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