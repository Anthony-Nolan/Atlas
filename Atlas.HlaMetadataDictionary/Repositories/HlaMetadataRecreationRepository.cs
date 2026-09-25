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
        Task<IReadOnlyDictionary<string, string>> GetRecreationStamps(CancellationToken cancellationToken = default);
    }

    internal class HlaMetadataRecreationRepository : IHlaMetadataRecreationRepository
    {
        private const string TableReference = "HlaMetadataDictionaryRecreations";

        private readonly ITableClientFactory factory;

        public HlaMetadataRecreationRepository(ITableClientFactory factory)
        {
            this.factory = factory;
        }

        public async Task RecordRecreation(string hlaNomenclatureVersion)
        {
            var tableClient = await factory.GetTable(TableReference);

            await tableClient.UpsertEntityAsync(
                new HlaMetadataRecreationRow(hlaNomenclatureVersion),
                TableUpdateMode.Replace);
        }

        public async Task<IReadOnlyDictionary<string, string>> GetRecreationStamps(CancellationToken cancellationToken = default)
        {
            var tableClient = await factory.GetTable(TableReference);

            var stamps = new Dictionary<string, string>();

            var rows = tableClient.QueryAsync<HlaMetadataRecreationRow>(
                row => row.PartitionKey == HlaMetadataRecreationRow.GetPartition(),
                cancellationToken: cancellationToken);

            await foreach (var row in rows)
            {
                stamps[row.RowKey] = row.Stamp;
            }

            return stamps;
        }
    }
}
