using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Functions.Models;
using AutoMapper;

namespace Atlas.Functions.Mappings
{
    public class MatchingResultsNotificationProfile : Profile
    {
        public MatchingResultsNotificationProfile()
        {
            CreateMap<MatchingResultsNotification, FailureNotificationRequestInfo>()
                .ForMember(dest => dest.StageReached, opt => opt.Ignore())
                .ForMember(dest => dest.MatchingAlgorithmFailureInfo, opt => opt.MapFrom(src => src.FailureInfo));
        }
    }
}