using AutoMapper;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models;

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