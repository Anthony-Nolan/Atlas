using Atlas.MatchingAlgorithm.Data.Persistent.Models;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;

/// <summary>
/// The data refresh that a donor genotype precomputation worker serves, and the transient database that the refresh
/// fills. The worker reads it once, when it starts, and writes every batch to that database until it stops.
/// </summary>
/// <remarks>
/// The database of a refresh record does not change, and the refresh swaps the databases only after the stage has
/// finished every batch. So the target is correct for the whole stage. A worker must stop before the next refresh starts:
/// the next refresh can fill the other database.
/// </remarks>
public sealed record DonorGenotypePrecomputationTarget(int DataRefreshRecordId, TransientDatabase Database);
