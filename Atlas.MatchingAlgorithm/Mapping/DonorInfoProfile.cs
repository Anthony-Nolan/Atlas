using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using AutoMapper;

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