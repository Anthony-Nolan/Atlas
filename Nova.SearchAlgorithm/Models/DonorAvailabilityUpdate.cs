namespace Nova.SearchAlgorithm.Models
{
    public class DonorAvailabilityUpdate
    {
        public int DonorId { get; set; }
        public bool IsAvailableForSearch { get; set; }
        public DonorInfo DonorInfo { get; set; }
    }
}