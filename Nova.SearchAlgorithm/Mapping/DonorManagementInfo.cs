using AutoMapper;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Models;

namespace Nova.SearchAlgorithm.Mapping
{
    public class DonorManagementInfoProfile : Profile
    {
        public DonorManagementInfoProfile()
        {
            CreateMap<DonorAvailabilityUpdate, DonorManagementInfo>();
        }
    }
}