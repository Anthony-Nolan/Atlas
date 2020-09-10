using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Microsoft.Azure.Cosmos.Table;

namespace Atlas.HlaMetadataDictionary.Repositories.AzureStorage
{
    internal class CloudTableBatchQueryAsync<TTableEntity> : IBatchQueryAsync<TTableEntity> 
        where TTableEntity : TableEntity, new()
    {
        private readonly TableQuery<TTableEntity> query;
        private readonly CloudTable table;

        private TableContinuationToken continuationToken = null;

        public CloudTableBatchQueryAsync(CloudTable table)
        {
            this.query = new TableQuery<TTableEntity>();
            this.table = table;
        }

        public bool HasMoreResults { get; private set; } = true;

        public async Task<IEnumerable<TTableEntity>> RequestNextAsync()
        {
            if (!HasMoreResults)
            {
                throw new Exception("More table results were requested even though no more results are available. " +
                                    "Check HasMoreResults before calling RequestNextAsync.");
            }

            var tableQueryResult = await table.ExecuteQuerySegmentedAsync(query, continuationToken);

            continuationToken = tableQueryResult.ContinuationToken;

            if (continuationToken == null)
            {
                HasMoreResults = false;
            }

            return tableQueryResult.Results;
        }
    }
}
