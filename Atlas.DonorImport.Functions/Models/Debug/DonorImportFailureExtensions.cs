using Atlas.Debug.Client.Models.DonorImport;
using Atlas.DonorImport.Data.Models;

namespace Atlas.DonorImport.Functions.Models.Debug
{
    internal static class DonorImportFailureExtensions
    {
        public static DonorUpdateFailureInfo ToDonorUpdateFailureInfo(this DonorImportFailure failure)
        {
            return new DonorUpdateFailureInfo
            {
                DonorImportFileName = failure.UpdateFile,
                Donor = new DonorUpdateFailureInfo.DonorInfo
                {
                    ExternalDonorCode = failure.ExternalDonorCode,
                    DonorType = failure.DonorType,
                    EthnicityCode = failure.EthnicityCode,
                    RegistryCode = failure.RegistryCode,
                },
                UpdateFailureInfo = new DonorUpdateFailureInfo.FailureInfo
                {
                    PropertyName = failure.UpdateProperty,
                    Reason = failure.FailureReason,
                    DateTime = failure.FailureTime
                }
            };
        }
    }
}
