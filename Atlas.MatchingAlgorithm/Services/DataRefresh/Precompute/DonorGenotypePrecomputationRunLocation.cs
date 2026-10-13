#nullable enable

using Atlas.MatchingAlgorithm.Data.Persistent.Models;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;

/// <summary>
/// Where a run of the donor genotype precomputation stage is: its data refresh record, the transient database that the
/// refresh fills, and the run id in that database. The batch messages of the run carry the record id and the run id. The
/// database is on the refresh record.
/// </summary>
public sealed record DonorGenotypePrecomputationRunLocation(int DataRefreshRecordId, TransientDatabase TargetDatabase, int RunId);
