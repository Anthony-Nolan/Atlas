using System;

namespace Atlas.Debug.Client.Models.DonorImport
{
    /// <summary>
    /// Information about why a donor update failed validation during donor import.
    /// </summary>
    public class DonorUpdateFailureInfo
    {
        public string DonorImportFileName { get; set; }
        public DonorInfo Donor { get; set; }
        public FailureInfo UpdateFailureInfo { get; set; }

        public class DonorInfo
        {
            public string ExternalDonorCode { get; set; }
            public string DonorType { get; set; }
            public string EthnicityCode { get; set; }
            public string RegistryCode { get; set; }
        }

        public class FailureInfo
        {
            public string PropertyName { get; set; }
            public string Reason { get; set; }
            public DateTimeOffset DateTime { get; set; }
        }
    }
}
