using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;

namespace Atlas.MatchingAlgorithm.PrecomputeWorker;

/// <summary>Reads the target of a batch message: the transient database of the data refresh record that the message names.</summary>
/// <remarks>
/// <para>
/// <b>For each message, not when the worker starts.</b> Outside a data refresh no record is open, and the worker must still
/// start and report healthy. A record can also close while its messages wait, for example when its refresh fails, and the
/// next message then finds it closed.
/// </para>
///
/// <para>
/// <b>The record that the message names, not the one open record.</b> The worker does not have to guess the record, and a
/// worker that still runs from an earlier refresh cannot write to the database of a later one. The dead-letter trigger finds
/// the database in the same way, and the claim of the batch then checks the record and the status of its run.
/// </para>
///
/// <para>
/// The database of a record does not change, and the refresh swaps the databases only after the stage has finished every
/// batch. So the database of the record is correct for every message of the stage.
/// </para>
/// </remarks>
internal static class DonorGenotypePrecomputationTargetReader
{
    /// <returns>
    /// The database of the record, when the record is open. Null when the record is closed or does not exist: its refresh
    /// has ended, and the batch is not the worker's to take.
    /// </returns>
    public static async Task<TransientDatabase?> ReadDatabase(IDataRefreshHistoryRepository dataRefreshHistoryRepository, int dataRefreshRecordId)
    {
        var openRecordDatabases = await dataRefreshHistoryRepository.GetIncompleteRefreshJobDatabases();
        return openRecordDatabases.TryGetValue(dataRefreshRecordId, out var database) ? database : null;
    }
}
