using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using EnumStringValues;

namespace Atlas.MatchingAlgorithm.PrecomputeWorker;

/// <summary>Reads the target of the worker: the open data refresh record, and the database that the refresh fills.</summary>
internal static class DonorGenotypePrecomputationTargetReader
{
    /// <exception cref="InvalidOperationException">
    /// No refresh record is open, or more than one is. The worker then does not start: it cannot tell which database to
    /// write.
    /// </exception>
    public static DonorGenotypePrecomputationTarget Read(IDataRefreshHistoryRepository dataRefreshHistoryRepository)
    {
        var openRecords = dataRefreshHistoryRepository.GetIncompleteRefreshJobs().ToList();
        if (openRecords.Count == 0)
        {
            throw new InvalidOperationException("No data refresh record is open, so the worker cannot tell which database to write.");
        }

        // The requester starts no refresh while another is open, so this is a fault of the refresh history.
        if (openRecords.Count > 1)
        {
            throw new InvalidOperationException(
                $"{openRecords.Count} data refresh records are open ({string.Join(", ", openRecords.Select(record => record.Id))}), " +
                "so the worker cannot tell which database to write.");
        }

        var openRecord = openRecords[0];
        return new DonorGenotypePrecomputationTarget(openRecord.Id, openRecord.Database.ParseToEnum<TransientDatabase>());
    }
}
