using Nova.SearchAlgorithm.Client.Models.Donors;

namespace Nova.SearchAlgorithm.Models
{
    public class DonorAvailabilityUpdate
    {
        public int DonorId { get; set; }
        public bool IsAvailableForSearch { get; set; }
        public InputDonor DonorInfo { get; set; }
    }
}