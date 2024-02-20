using System;
using System.Collections.Generic;

namespace Atlas.Debug.Client.Models.DonorImport
{
    /// <summary>
    /// Information about why a donor update failed validation during donor import.
    /// </summary>
    public class DonorImportFailureInfo
    {
        /// <summary>
        /// Name of donor import file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Number of updates that failed to be applied.
        /// </summary>
        public int FailedUpdateCount { get; set; }

        /// <summary>
        /// Details of each failed update.
        /// </summary>
        public IEnumerable<FailedDonorUpdate> FailedUpdates { get; set; }
    }

    /// <summary>
    /// Info on a donor update failed to be imported into the donor store.
    /// </summary>
    public class FailedDonorUpdate : IEquatable<FailedDonorUpdate>
    {
        public string ExternalDonorCode { get; set; }
        public string DonorType { get; set; }
        public string EthnicityCode { get; set; }
        public string RegistryCode { get; set; }

        /// <summary>
        /// Name of property that failed validation.
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Reason why <see cref="PropertyName"/> failed validation.
        /// </summary>
        public string FailureReason { get; set; }

        /// <summary>
        /// Date and time of the update failure.
        /// </summary>
        public DateTimeOffset FailureDateTime { get; set; }

        #region Equality members

        /// <summary><inheritdoc />
        /// Note: FailureDateTime is purposely excluded from the equality comparison.</summary>
        public bool Equals(FailedDonorUpdate other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                ExternalDonorCode == other.ExternalDonorCode && 
                DonorType == other.DonorType && 
                EthnicityCode == other.EthnicityCode && 
                RegistryCode == other.RegistryCode && 
                PropertyName == other.PropertyName && 
                FailureReason == other.FailureReason;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FailedDonorUpdate)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(ExternalDonorCode, DonorType, EthnicityCode, RegistryCode, PropertyName, FailureReason, FailureDateTime);
        }

        #endregion
    }
}
