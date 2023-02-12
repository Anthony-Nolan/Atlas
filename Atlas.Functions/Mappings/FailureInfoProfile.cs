using Atlas.Client.Models.Search.Results;
using Atlas.Functions.Models;
using AutoMapper;

namespace Atlas.Functions.Mappings
{
    public class FailureInfoProfile : Profile
    {
        public FailureInfoProfile()
        {
            CreateMap<FailureNotificationRequestInfo, SearchFailureInfo>();
        }
    }
}