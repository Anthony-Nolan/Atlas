using Atlas.Client.Models.Common.Requests;
using Atlas.Client.Models.Scoring;
using Atlas.Client.Models.Scoring.Requests;
using Atlas.Client.Models.Scoring.Results;
using AutoMapper;

namespace Atlas.Functions.PublicApi.Mappings
{
    public class ScoringProfile : Profile
    {
        public ScoringProfile()
        {
            CreateMap<DonorHlaScoringRequest, MatchingAlgorithm.Client.Models.Scoring.DonorHlaScoringRequest>();
            CreateMap<DonorHlaBatchScoringRequest, MatchingAlgorithm.Client.Models.Scoring.BatchScoringRequest>();
            CreateMap<MatchingAlgorithm.Client.Models.Scoring.ScoringResult, ScoringResult>();
            CreateMap<MatchingAlgorithm.Client.Models.Scoring.DonorScoringResult, DonorScoringResult>();
            CreateMap<ScoringCriteria, Client.Models.Common.Requests.ScoringCriteria>();
            CreateMap<IdentifiedDonorHla, MatchingAlgorithm.Client.Models.Scoring.IdentifiedDonorHla>();
        }
    }
}