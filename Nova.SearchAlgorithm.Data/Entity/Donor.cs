using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace Nova.SearchAlgorithm.Data.Entity
{
    public class Donor
    {
        [Key]
        public int DonorId { get; set; }

        // TODO:NOVA-929 make donor types a strongly typed Enum
        public string DonorType { get; set; }
        // TODO:NOVA-931 this might need to be a string?
        public RegistryCode RegistryCode { get; set; }

        public SearchableDonor ToSearchableDonor()
        {
            return new SearchableDonor
            {
                DonorId = DonorId,
                DonorType = DonorType,
                RegistryCode = RegistryCode
            };
        }
    }
}