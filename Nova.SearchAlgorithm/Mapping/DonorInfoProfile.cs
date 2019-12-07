using AutoMapper;
using Nova.SearchAlgorithm.Data.Models.DonorInfo;

namespace Nova.SearchAlgorithm.Mapping
{
    public class DonorInfoProfile : Profile
    {
        public DonorInfoProfile()
        {
            CreateMap<DonorInfoWithExpandedHla, DonorInfo>();
        }
    }
}