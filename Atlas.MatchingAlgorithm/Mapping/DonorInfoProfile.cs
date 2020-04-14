using AutoMapper;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;

namespace Atlas.MatchingAlgorithm.Mapping
{
    public class DonorInfoProfile : Profile
    {
        public DonorInfoProfile()
        {
            CreateMap<DonorInfoWithExpandedHla, DonorInfo>();
        }
    }
}