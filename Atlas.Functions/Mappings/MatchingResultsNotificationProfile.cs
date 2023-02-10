using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Functions.Models;
using AutoMapper;

namespace Atlas.Functions.Mappings
{
    public class MatchingResultsNotificationProfile : Profile
    {
        public MatchingResultsNotificationProfile()
        {
            CreateMap<MatchingResultsNotification, DeliveredMatchingResultsNotification>();

            CreateMap<DeliveredMatchingResultsNotification, FailureNotificationRequestInfo>()
                .ForMember(dest => dest.StageReached, opt => opt.Ignore())
                .ForMember(dest => dest.WillNotBeRetried, opt => opt.Ignore())
                .ForMember(dest => dest.AttemptNumber, opt => opt.MapFrom(src => src.MessageDeliveryCount))
                .ForMember(dest => dest.MatchingAlgorithmValidationError, opt => opt.MapFrom(src => src.ValidationError));
        }
    }
}