namespace Atlas.Common.Public.Models.MatchPrediction
{
    /// <summary>
    /// This is the data used to determine which frequency set to use, for both Donors and Patients.
    /// </summary>
    public class FrequencySetMetadata
    {
        public string EthnicityCode { get; set; }
        public string RegistryCode { get; set; }
        
        #region Equality members

        protected bool Equals(FrequencySetMetadata other)
        {
            return EthnicityCode == other.EthnicityCode && RegistryCode == other.RegistryCode;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((FrequencySetMetadata) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // TODO: ATLAS-632: Make this class immutable (prioritise this if class ever used as a dictionary key)
            return HashCode.Combine(EthnicityCode, RegistryCode);
        }

        public static bool operator ==(FrequencySetMetadata left, FrequencySetMetadata right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FrequencySetMetadata left, FrequencySetMetadata right)
        {
            return !Equals(left, right);
        }

        #endregion

    }
}