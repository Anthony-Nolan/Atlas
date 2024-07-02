using System.Collections.Generic;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.MatchingAlgorithm.Common.Models.Matching
{
    /// <summary>
    /// Used to specify what level of donor filtering should be performed in the initial SQL query
    /// There is a performance trade-off between adding an extra JOIN to the donor table, vs. scanning more MatchingHlaAtX rows for p-groups
    /// </summary>
    public class MatchingFilteringOptions
    {
        /// <summary>
        /// When set, will filter the SQL query allowing only the specified donor type.
        /// Otherwise, allows all donor types.
        /// </summary>
        public DonorType? DonorType { get; set; }

        public bool ShouldFilterOnDonorType => DonorType != null;
        
        /// <summary>
        /// When set, will filter the SQL query allowing only provided donor Ids.
        /// Otherwise, allows all donors. 
        /// </summary>
        public ICollection<int>? DonorIds { get; set; }

        public bool ShouldFilterOnDonorIds => DonorIds != null && DonorIds.Count > 0;

        public ICollection<string>? RegistryCodes { get; set; }

        public bool ShouldFilterOnRegistryCodeIds => RegistryCodes != null && RegistryCodes.Count > 0;
    }
}