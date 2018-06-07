using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Exceptions;

namespace Nova.SearchAlgorithm.Repositories.Donors.AzureStorage
{
    public class CloudTableDonorBatchQueryAsync : IBatchQueryAsync<DonorResult>
    {
        private readonly TableQuery<DonorTableEntity> query;
        private readonly CloudTable table;

        private TableContinuationToken continuationToken = null;

        public CloudTableDonorBatchQueryAsync(TableQuery<DonorTableEntity> query, CloudTable table)
        {
            this.query = query;
            this.table = table;
        }
        
        public bool HasMoreResults { get; private set; } = true;

        public async Task<IEnumerable<DonorResult>> RequestNextAsync()
        {
            if (!HasMoreResults)
            {
                throw new CloudStorageException("More donors were requested even though no more results are available. Check HasMoreResults before calling RequestNextAsync.");
            }

            TableQuerySegment<DonorTableEntity> tableQueryResult =
                await table.ExecuteQuerySegmentedAsync(query, continuationToken);

            continuationToken = tableQueryResult.ContinuationToken;

            if (continuationToken == null)
            {
                HasMoreResults = false;
            }

            return tableQueryResult.Results.Select(d => d.ToDonorResult());
        }
    }
}