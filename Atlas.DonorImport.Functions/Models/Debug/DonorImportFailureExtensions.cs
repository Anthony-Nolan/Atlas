using Atlas.Debug.Client.Models.DonorImport;
using Atlas.DonorImport.Data.Models;

namespace Atlas.DonorImport.Functions.Models.Debug
{
    internal static class DonorImportFailureExtensions
    {
        public static FailedDonorUpdate ToFailedDonorUpdate(this DonorImportFailure failure)
        {
            return new FailedDonorUpdate
            {
                ExternalDonorCode = failure.ExternalDonorCode,
                DonorType = failure.DonorType,
                EthnicityCode = failure.EthnicityCode,
                RegistryCode = failure.RegistryCode,
                PropertyName = failure.UpdateProperty,
                FailureReason = failure.FailureReason,
                FailureDateTime = failure.FailureTime
            };
        }
    }
}
