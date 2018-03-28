using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Models
{
    public class ImportDonor
    {
        public string DonorId { get; set; }
        // TODO:NOVA-929 make donor types a strongly typed Enum
        public string DonorType { get; set; }
        public RegistryCode RegistryCode { get; set; }
        public MatchingHla LocusA { get; set; }
    }
}