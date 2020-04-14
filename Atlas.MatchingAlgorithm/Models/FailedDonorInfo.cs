namespace Atlas.MatchingAlgorithm.Models
{
    public class FailedDonorInfo
    {
        public string DonorId { get; set; }
        public string RegistryCode { get; set; }
        public object DonorInfo { get; set; }

        public FailedDonorInfo()
        {
        }

        public FailedDonorInfo(object donorInfo)
        {
            DonorInfo = donorInfo;
        }
    }
}
