using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;

namespace Atlas.MatchingAlgorithm.Models
{
    public static class DonorInfoExtensions
    {
        public static FailedDonorInfo ToFailedDonorInfo(this DonorInfo donorInfo)
        {
            return new FailedDonorInfo(donorInfo)
            {
                AtlasDonorId = donorInfo.DonorId
            };
        }
    }
}
