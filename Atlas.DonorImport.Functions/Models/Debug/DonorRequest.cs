using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.DonorImport.Functions.Models.Debug
{
    public class DonorRequest
    {
        /// <summary>
        /// Optional parameter if you wish to limit donors to specified <see cref="DonorType"/>
        /// </summary>
        public ImportDonorType? DonorType { get; set; }

        /// <summary>
        /// Optional parameter if you wish to limit donor to specified <see cref="RegistryCode"/>
        /// </summary>
        public string RegistryCode { get; set; }
    }
}
