using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Cosmos.Table.Protocol;
using MoreLinq;

namespace Atlas.Common.AzureStorage.TableStorage
{
    /// <summary>
    /// Microsoft has 2 No-Sql cloud storage offerings: Azure Cosmos Storage ("ACS") and Azure Table Storage ("ATS").
    /// These two services are very similar, and have almost identical logical models, but are subtly different.
    /// 
    /// For reasons best not dwelt upon, Microsoft have migrated the actively supported code for managing "ATS" from
    /// the `Microsoft.WindowsAzure.Storage` library / nuget package ("M.WA.S"), and combined it into
    /// the `Microsoft.Azure.Cosmos` library / nuget package ("M.A.C").
    ///
    /// The "M.A.C" library now contains code to manage *both* "ACS" *and* "ATS".
    /// The "M.WA.S" library is deprecated but still (currently) available.
    /// 
    /// We are ONLY using the ATS service, but (regrettably) have a mix of of the two libraries/packages.
    /// The M.A.C library, *does* entirely support ATS, and as far as we know the 2 libraries can be used side-by-side on the same tables and objects.
    /// However, since the 2 libraries don't share any types or interfaces, the classes & methods don't interoperate.
    ///
    /// We have some code that was originally written using the M.WA.S library, and hasn't been updated.
    /// And we have some newer code that has been written using the M.A.C library.
    /// We hope in due course to migrate everything to using the M.A.C library (but to continue using the ATS service) (TODO: ATLAS-485)
    /// For the moment, we just try to keep these extension methods in sync where necessary.
    /// </summary>
    public static class AzureCosmosLibraryTableExtensions
    {
        public static async Task<IList<T>> ExecuteQueryAsync<T>(
            this CloudTable table,
            TableQuery<T> query,
            CancellationToken ct = default,
            Action<IList<T>> onProgress = null)
            where T : ITableEntity, new()
        {
            var items = new List<T>();
            TableContinuationToken token = null;

            do
            {
                var seg = await table.ExecuteQuerySegmentedAsync<T>(query, token, ct);
                token = seg.ContinuationToken;
                items.AddRange(seg);
                onProgress?.Invoke(items);

            } while (token != null && !ct.IsCancellationRequested);

            return items;
        }

        private const int BatchSize = TableConstants.TableServiceBatchMaximumOperations; //ExecuteBatchAsync has a limit on how many operations can be put in a single batch. :(

        /*
         * Note that the internet recommends the following settings for optimal AzureTableStorage Insert performance.
         * However, applying them didn't appear to have any impact. Possibly because we're batching all our inserts, which negates the issues these address?
         *
         * ServicePointManager.Expect100Continue = false;
         * ServicePointManager.UseNagleAlgorithm = false;
         * ServicePointManager.DefaultConnectionLimit = 100;
         */
        public static async Task BatchInsert<TEntity>(this CloudTable table, IEnumerable<TEntity> entities)
            where TEntity : TableEntity
        {
            // ReSharper disable once SuggestVarOrType_Elsewhere
            // List<List<List<TEntities>>> 
            // This construct is:
            // * A List of Partitions, each containing
            //   * List of 100-entity-batches within that partition, each containing
            //     * List of (100) Entities within that batch (all of which have a single PartitionKey)
            //
            // We start from this construct as it allows easy refactors of how we divvy these inserts up, in the future.
            List<List<List<TEntity>>> entitiesPartitionedWithSubBatches = entities
                .GroupBy(e => e.PartitionKey) //Note that batch inserts MUST have a common Partition Key.
                .Select(partitionGroup => partitionGroup.Batch(BatchSize).Select(batch => batch.ToList()).ToList())
                .ToList();

            foreach (var subBatchesWithinASinglePartition in entitiesPartitionedWithSubBatches)
            {
                foreach (var batchToInsert in subBatchesWithinASinglePartition)
                {
                    var batchOperation = new TableBatchOperation();
                    foreach (var tableEntity in batchToInsert)
                    {
                        batchOperation.Insert(tableEntity);
                    }

                    try
                    {
                        await table.ExecuteBatchAsync(batchOperation);
                    }
                    catch (StorageException ex)
                    {
                        throw new AzureTableBatchInsertException(batchToInsert, ex);
                    }
                }
            }
        }
    }
}
