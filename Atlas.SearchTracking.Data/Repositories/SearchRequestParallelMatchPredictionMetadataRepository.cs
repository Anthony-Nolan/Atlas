using Atlas.SearchTracking.Data.Context;
using Atlas.SearchTracking.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Atlas.SearchTracking.Data.Repositories;

public interface ISearchRequestParallelMatchPredictionMetadataRepository
{
    /// <summary>Creates a new metadata row before batch messages are dispatched.</summary>
    Task<int> Create(Guid searchIdentifier,
        bool isRepeatSearch,
        Guid? repeatSearchIdentifier,
        string resultsFileName,
        bool resultsBatched,
        string? batchFolderName,
        TimeSpan matchingAlgorithmElapsedTime,
        DateTime searchInitiatedTimeUtc,
        int totalBatchCount);

    /// <summary>
    /// In a single transaction: bulk-inserts the donor result locations for this batch, then
    /// atomically increments <c>ProcessedBatchCount</c> using a raw SQL UPDATE (no read-modify-write).
    /// </summary>
    Task<int> AddBatchResultAndIncrementCount(int parallelMetadataId,
        IReadOnlyDictionary<int, string> resultLocations);

    Task<SearchRequestParallelMatchPredictionMetadata?> Find(int messageParallelMetadataId);

    Task<SearchRequestParallelMatchPredictionMetadata> GetWithLocations(int messageParallelMetadataId);
}

public class SearchRequestParallelMatchPredictionMetadataRepository : ISearchRequestParallelMatchPredictionMetadataRepository
{
    private readonly ISearchTrackingContext context;

    private DbContext DbContext => (DbContext)context;

    private DbSet<SearchRequestParallelMatchPredictionMetadata> Metadata =>
        context.SearchRequestParallelMatchPredictionMetadata;

    private DbSet<SearchRequestParallelMatchPredictionResultLocation> ResultLocations =>
        context.SearchRequestParallelMatchPredictionResultLocations;

    public SearchRequestParallelMatchPredictionMetadataRepository(ISearchTrackingContext context)
    {
        this.context = context;
    }

    public async Task<int> Create(
        Guid searchIdentifier,
        bool isRepeatSearch,
        Guid? repeatSearchIdentifier,
        string resultsFileName,
        bool resultsBatched,
        string? batchFolderName,
        TimeSpan matchingAlgorithmElapsedTime,
        DateTime searchInitiatedTimeUtc,
        int totalBatchCount)
    {
        var entity = new SearchRequestParallelMatchPredictionMetadata
        {
            SearchIdentifier = searchIdentifier,
            IsRepeatSearch = isRepeatSearch,
            RepeatSearchIdentifier = repeatSearchIdentifier,
            ResultsFileName = resultsFileName,
            ResultsBatched = resultsBatched,
            BatchFolderName = batchFolderName,
            MatchingAlgorithmElapsedTime = matchingAlgorithmElapsedTime,
            SearchInitiatedTimeUtc = searchInitiatedTimeUtc,
            TotalBatchCount = totalBatchCount,
            ProcessedBatchCount = 0
        };
        Metadata.Add(entity);

        await context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<int> AddBatchResultAndIncrementCount(int parallelMetadataId,
        IReadOnlyDictionary<int, string> resultLocations)
    {
        await using var transaction = await DbContext.Database.BeginTransactionAsync();

        int? processedBatchCount;
        try
        {
            // Idempotency guard: if any donors from this batch are already persisted, this message
            // was already processed (Service Bus retry/duplicate). Return the current count unchanged.
            var donorIds = resultLocations.Keys.ToList();
            var alreadyInsertedCount = await ResultLocations
                .CountAsync(r => r.MetadataId == parallelMetadataId && donorIds.Contains(r.DonorId));

            if (alreadyInsertedCount > 0)
            {
                processedBatchCount = await Metadata
                    .Where(m => m.Id == parallelMetadataId)
                    .Select(m => m.ProcessedBatchCount)
                    .SingleOrDefaultAsync();
                await transaction.CommitAsync();
                return processedBatchCount.Value;
            }

            // Atomically increment ProcessedBatchCount. OUTPUT returns no rows (default 0) when the
            // metadata row does not exist, allowing us to detect the missing-row case without a
            // separate query.
            // Can't use fancy .FromSql or anything else because of EF Core shenanigans
            var command = DbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText =
                $"""
                 UPDATE [{SearchTrackingContext.Schema}].[SearchRequestParallelMatchPredictionMetadata]
                 SET ProcessedBatchCount = ProcessedBatchCount + 1
                 OUTPUT inserted.ProcessedBatchCount
                 WHERE Id = @parallelMetadataId
                 """;
            command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@parallelMetadataId", parallelMetadataId));
            // Transaction is not set on the command by default, so we have to do it manually.
            command.Transaction = transaction.GetDbTransaction();

            processedBatchCount = (int?)await command.ExecuteScalarAsync();

            if (processedBatchCount is null or 0)
            {
                throw new InvalidOperationException(
                    $"No parallel match prediction metadata found with id '{parallelMetadataId}'."
                );
            }

            ResultLocations.AddRange(resultLocations.Select(kv =>
                    new SearchRequestParallelMatchPredictionResultLocation
                    {
                        MetadataId = parallelMetadataId,
                        DonorId = kv.Key,
                        ResultBlobFileName = kv.Value
                    }
                )
            );

            await context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return processedBatchCount.Value;
    }

    /// <inheritdoc />
    public async Task<SearchRequestParallelMatchPredictionMetadata?> Find(int messageParallelMetadataId)
    {
        return await Metadata.FirstOrDefaultAsync(m => m.Id == messageParallelMetadataId);
    }

    /// <inheritdoc />
    public async Task<SearchRequestParallelMatchPredictionMetadata> GetWithLocations(int messageParallelMetadataId)
    {
        return await Metadata.Include(m => m.ResultLocations)
            .SingleAsync(m => m.Id == messageParallelMetadataId);
    }
}