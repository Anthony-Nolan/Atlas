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
        private const int BatchSize = 1000;

        // Note that giving this class an IQueryable rather than IEnumerable will leave an open db connection through EF
        // No other IO can be performed by Entity Framework while this is the case - may be worth investigating using IEnumerable instead
        public SqlDonorBatchQueryAsync(IEnumerable<Donor> donors)
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
                var donors = new List<DonorResult>();
                for (var i = 0; i < BatchSize; i++)
                {
                    if (HasMoreResults)
                    {
                        donors.Add(enumerator.Current.ToDonorResult());
                        HasMoreResults = enumerator.MoveNext();
                    }
                }

                return donors.AsEnumerable();
            });
        }
    }
}