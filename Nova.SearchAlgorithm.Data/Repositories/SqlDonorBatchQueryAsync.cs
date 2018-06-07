using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Data.Exceptions;

namespace Nova.SearchAlgorithm.Data.Repositories
{
    public class SqlDonorBatchQueryAsync : IBatchQueryAsync<DonorResult>
    {
        private readonly IEnumerator<Donor> enumerator;

        public SqlDonorBatchQueryAsync(IQueryable<Donor> donors)
        {
            enumerator = donors.GetEnumerator();
            HasMoreResults = enumerator.MoveNext();
        }

        public bool HasMoreResults { get; private set; }

        public Task<IEnumerable<DonorResult>> RequestNextAsync()
        {
            if (!HasMoreResults)
            {
                throw new DataHttpException("More donors were requested even though no more results are available. Check HasMoreResults before calling RequestNextAsync.");
            }

            return Task.Run(() =>
            {
                var donor = enumerator.Current;
                HasMoreResults = enumerator.MoveNext();
                return new List<DonorResult> { donor.ToDonorResult() }.AsEnumerable();
            });
        }
    }
}