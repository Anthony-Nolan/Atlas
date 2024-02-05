using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.Debug.Client.Models.DonorImport
{
    /// <summary>
    /// Info of a donor that was found in the target donor database during a debug request.
    /// </summary>
    public class DonorDebugInfo
    {
        /// <summary>
        /// Equivalent to recordId in the donor import file
        /// </summary>
        public string ExternalDonorCode { get; set; }

        public string DonorType { get; set; }
        public string RegistryCode { get; set; }
        public string EthnicityCode { get; set; }
        public PhenotypeInfoTransfer<string> Hla { get; set; }
    }
}