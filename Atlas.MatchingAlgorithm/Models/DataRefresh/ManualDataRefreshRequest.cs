using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;

namespace Atlas.MatchingAlgorithm.Models.DataRefresh;

/// <summary>
/// The body of the <c>SubmitDataRefreshRequestManual</c> endpoint. ATL-491 experiment only: it adds
/// <see cref="JustPrecompute"/> to the public <see cref="DataRefreshRequest"/>, which stays as it is.
/// </summary>
public class ManualDataRefreshRequest : DataRefreshRequest
{
    /// <summary>
    /// Requires <see cref="DataRefreshRequest.ForceDataRefresh"/>. The refresh skips donor import, HLA processing and the
    /// index stages, removes only the precomputed data of the dormant database, and runs the donor genotype precomputation
    /// stage for a sample of its donors. It then pauses, and never swaps the databases.
    /// </summary>
    public bool JustPrecompute { get; set; }
}
