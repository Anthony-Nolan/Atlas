using Atlas.Client.Models.Scoring;
using AutoMapper;

namespace Atlas.Functions.PublicApi.Mappings
{
    public class ScoringProfile : Profile
    {
        public ScoringProfile()
        {
            CreateMap<DonorHlaScoringRequest, MatchingAlgorithm.Client.Models.Scoring.DonorHlaScoringRequest>();
            CreateMap<BatchScoringRequest, MatchingAlgorithm.Client.Models.Scoring.BatchScoringRequest>();
            CreateMap<MatchingAlgorithm.Client.Models.Scoring.ScoringResult, ScoringResult>();
            CreateMap<MatchingAlgorithm.Client.Models.Scoring.DonorScoringResult, DonorScoringResult>();
            CreateMap<ScoringCriteria, Client.Models.Search.Requests.ScoringCriteria>();
            CreateMap<IdentifiedDonorHla, MatchingAlgorithm.Client.Models.Scoring.IdentifiedDonorHla>();
        }
    }
}