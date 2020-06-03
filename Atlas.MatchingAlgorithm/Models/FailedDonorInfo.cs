namespace Atlas.MatchingAlgorithm.Models
{
    public class FailedDonorInfo
    {
        /// <summary>
        /// The Atlas ID of the failed donor - this corresponds to the PK of the donor in the master Atlas "donor import" database
        /// </summary>
        public int? AtlasDonorId { get; set; }
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
