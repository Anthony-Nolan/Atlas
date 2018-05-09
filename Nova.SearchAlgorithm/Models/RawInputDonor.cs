using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Models
{
    /// <summary>
    /// This class represents raw donor data from some outside source
    /// such as Solar or a donor repository.
    /// </summary>
    public class RawInputDonor
    {
        public int DonorId { get; set; }

        // This might not match our own idea of registry codes, e.g. "AN" vs "ANBMT"
        public string RegistryCode { get; set; }

        // This might not match our own idea of types, e.g. "A" vs "Adult"
        public string DonorType { get; set; }

        public PhenotypeInfo<string> HlaNames { get; set; }
    }
}