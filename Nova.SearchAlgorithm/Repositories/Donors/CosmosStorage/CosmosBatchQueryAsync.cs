using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Linq;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Repositories.Donors.CosmosStorage
{
    public class CosmosDonorResultBatchQueryAsync : IBatchQueryAsync<DonorResult>
    {
        private readonly IDocumentQuery<DonorCosmosDocument> query;

        public CosmosDonorResultBatchQueryAsync(IDocumentQuery<DonorCosmosDocument> query)
        {
            this.query = query;
        }
        
        public bool HasMoreResults => query.HasMoreResults;

        public async Task<IEnumerable<DonorResult>> RequestNextAsync()
        {
            return (await query.ExecuteNextAsync<DonorCosmosDocument>()).Select(d => d.ToDonorResult());
        }
    }
}