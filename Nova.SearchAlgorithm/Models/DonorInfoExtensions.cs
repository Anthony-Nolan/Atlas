using Nova.SearchAlgorithm.Data.Models.DonorInfo;

namespace Nova.SearchAlgorithm.Models
{
    public static class DonorInfoExtensions
    {
        public static FailedDonorInfo ToFailedDonorInfo(this DonorInfo donorInfo)
        {
            return new FailedDonorInfo(donorInfo)
            {
                DonorId = donorInfo.DonorId.ToString(),
                RegistryCode = donorInfo.RegistryCode.ToString()
            };
        }
    }
}
