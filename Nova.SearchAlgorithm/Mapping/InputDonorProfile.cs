using AutoMapper;
using Nova.SearchAlgorithm.Data.Models.DonorInfo;

namespace Nova.SearchAlgorithm.Mapping
{
    public class InputDonorProfile : Profile
    {
        public InputDonorProfile()
        {
            CreateMap<DonorResult, InputDonor>();
        }
    }
}