namespace Nova.SearchAlgorithm.Models
{
    public class FailedDonorInfo
    {
        public string DonorId { get; set; }
        public string RegistryCode { get; set; }
        public object DonorInfo { get; }

        public FailedDonorInfo(object donorInfo)
        {
            DonorInfo = donorInfo;
        }
    }
}
