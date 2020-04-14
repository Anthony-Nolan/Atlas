namespace Atlas.MatchingAlgorithm.Common.Models.Matching
{
    /// <summary>
    /// Used to specify what level of donor filtering should be performed in the initial SQL query
    /// There is a performance trade-off between adding an extra JOIN to the donor table, vs. scanning more MatchingHlaAtX rows for p-groups
    /// </summary>
    public class MatchingFilteringOptions
    {
        public bool ShouldFilterOnRegistry { get; set; }
        public bool ShouldFilterOnDonorType { get; set; }
    }
}