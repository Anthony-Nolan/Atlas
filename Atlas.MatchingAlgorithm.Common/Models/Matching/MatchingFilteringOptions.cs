using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.Common.Models.Matching
{
    /// <summary>
    /// Used to specify what level of donor filtering should be performed in the initial SQL query
    /// There is a performance trade-off between adding an extra JOIN to the donor table, vs. scanning more MatchingHlaAtX rows for p-groups
    /// </summary>
    public class MatchingFilteringOptions
    {
        public bool ShouldFilterOnDonorType { get; set; }
        
        // TODO: ATLAS-714: Is it right for this to be in the filtering options but the type to be in the criteria?
        /// <summary>
        /// When set, will filter the SQL query allowing only provided donor Ids.
        /// Otherwise, allows all donors. 
        /// </summary>
        public HashSet<int> DonorIds { get; set; }
    }
}