using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;

namespace Atlas.MatchingAlgorithm.Models
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
