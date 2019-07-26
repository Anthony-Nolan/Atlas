using AutoMapper;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;

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