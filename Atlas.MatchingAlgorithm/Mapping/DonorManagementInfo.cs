using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Models;
using AutoMapper;

namespace Atlas.MatchingAlgorithm.Mapping
{
    public class DonorManagementInfoProfile : Profile
    {
        public DonorManagementInfoProfile()
        {
            CreateMap<DonorAvailabilityUpdate, DonorManagementInfo>();
        }
    }
}