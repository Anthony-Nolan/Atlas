using Atlas.SearchTracking.Data.Context;
using Atlas.SearchTracking.Data.Models;
using Microsoft.EntityFrameworkCore;

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

        int processedBatchCount;
        try
        {
            // Do
            processedBatchCount = await DbContext.Database.SqlQuery<int>(
                $"""
                 UPDATE [{SearchTrackingContext.Schema}].[SearchRequestParallelMatchPredictionMetadata]
                                        SET ProcessedBatchCount = ProcessedBatchCount + 1
                                        WHERE Id = {parallelMetadataId}
                                        OUTPUT inserted.ProcessedBatchCount
                 """
            ).SingleAsync();

            var metadataId = await Metadata
                .Where(m => m.Id == parallelMetadataId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync();

            if (metadataId == 0)
            {
                throw new InvalidOperationException(
                    $"No parallel match prediction metadata found with id '{parallelMetadataId}'."
                );
            }

            ResultLocations.AddRange(resultLocations.Select(kv =>
                    new SearchRequestParallelMatchPredictionResultLocation
                    {
                        MetadataId = metadataId,
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

        return processedBatchCount;
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