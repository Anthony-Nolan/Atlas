using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.InternalModels;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Azure.Data.Tables;

namespace Atlas.HlaMetadataDictionary.Repositories
{
    /// <summary>
    /// Records that the dictionary's stored data was recreated at a given nomenclature version, and reports the
    /// current stamps so a running process can notice.
    /// </summary>
    /// <remarks>
    /// Deliberately the same storage account, and the same connection, the dictionary's data itself uses: a process
    /// that can read the dictionary can already read this, so there are no new credentials and nothing to grant.
    /// </remarks>
    internal interface IHlaMetadataRecreationRepository
    {
        Task RecordRecreation(string hlaNomenclatureVersion);

        /// <returns>Nomenclature version to its current stamp.</returns>
        Task<IReadOnlyDictionary<string, HlaMetadataRecreationStamp>> GetRecreationStamps(CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether this process recorded that stamp itself - in which case it has already evicted that version's
        /// cached data directly, and has nothing further to invalidate.
        /// </summary>
        bool WasWrittenByThisProcess(string hlaNomenclatureVersion, string stamp);
    }

    /// <remarks>
    /// Must be registered as a singleton: the stamps this process has written are remembered in memory, and have to
    /// be visible to the watcher, which resolves the repository in a scope of its own.
    /// </remarks>
    internal class HlaMetadataRecreationRepository : IHlaMetadataRecreationRepository
    {
        private const string TableReference = "HlaMetadataDictionaryRecreations";

        private readonly ITableClientFactory factory;

        /// <summary>Nomenclature version to the stamp this process last wrote for it.</summary>
        private readonly ConcurrentDictionary<string, string> stampsWrittenByThisProcess = new();

        public HlaMetadataRecreationRepository(ITableClientFactory factory)
        {
            this.factory = factory;
        }

        public async Task RecordRecreation(string hlaNomenclatureVersion)
        {
            var row = new HlaMetadataRecreationRow(hlaNomenclatureVersion);

            // Remembered before it is written, so that a poll landing between the two cannot mistake it for another
            // process's. If the write then fails, the remembered stamp never appears in the table, so it matches nothing.
            stampsWrittenByThisProcess[hlaNomenclatureVersion] = row.Stamp;

            var tableClient = await factory.GetTable(TableReference);
            await tableClient.UpsertEntityAsync(row, TableUpdateMode.Replace);
        }

        public async Task<IReadOnlyDictionary<string, HlaMetadataRecreationStamp>> GetRecreationStamps(
            CancellationToken cancellationToken = default)
        {
            var tableClient = await factory.GetTable(TableReference);

            var stamps = new Dictionary<string, HlaMetadataRecreationStamp>();

            var rows = tableClient.QueryAsync<HlaMetadataRecreationRow>(
                row => row.PartitionKey == HlaMetadataRecreationRow.GetPartition(),
                cancellationToken: cancellationToken);

            await foreach (var row in rows)
            {
                stamps[row.RowKey] = new HlaMetadataRecreationStamp(row.Stamp, row.RecreatedAtUtc);
            }

            return stamps;
        }

        public bool WasWrittenByThisProcess(string hlaNomenclatureVersion, string stamp) =>
            stampsWrittenByThisProcess.TryGetValue(hlaNomenclatureVersion, out var written) && written == stamp;
    }
}
