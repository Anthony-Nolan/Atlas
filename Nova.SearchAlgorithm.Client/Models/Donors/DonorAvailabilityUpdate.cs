using Nova.DonorService.SearchAlgorithm.Models.DonorInfoForSearchAlgorithm;

namespace Nova.SearchAlgorithm.Client.Models.Donors
{
    public class DonorAvailabilityUpdate
    {
        public int DonorId { get; set; }
        public bool IsAvailableForSearch { get; set; }
        public DonorInfoForSearchAlgorithm DonorInfoForSearchAlgorithm { get; set; }
    }
}