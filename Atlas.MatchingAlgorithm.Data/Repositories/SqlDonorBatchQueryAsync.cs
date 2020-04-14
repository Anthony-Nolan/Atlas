using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Exceptions;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Data.Repositories
{
    public class SqlDonorBatchQueryAsync : IBatchQueryAsync<DonorInfo>
    {
        private readonly IEnumerator<Donor> enumerator;
        private const int DefaultBatchSize = 1000;

        private readonly int batchSize;

        // Note that giving this class an IQueryable rather than IEnumerable will leave an open db connection through EF
        // No other IO can be performed by Entity Framework while this is the case - do not change to IQueryable
        public SqlDonorBatchQueryAsync(IEnumerable<Donor> donors, int batchSize = DefaultBatchSize)
        {
            this.batchSize = batchSize;
            enumerator = donors.GetEnumerator();
            HasMoreResults = enumerator.MoveNext();
        }

        public bool HasMoreResults { get; private set; }

        public Task<IEnumerable<DonorInfo>> RequestNextAsync()
        {
            if (!HasMoreResults)
            {
                throw new DataHttpException("More donors were requested even though no more results are available. Check HasMoreResults before calling RequestNextAsync.");
            }

            return Task.Run(() =>
            {
                var donors = new List<DonorInfo>();
                for (var i = 0; i < batchSize; i++)
                {
                    if (HasMoreResults)
                    {
                        donors.Add(enumerator.Current.ToDonorInfo());
                        HasMoreResults = enumerator.MoveNext();
                    }
                }

                return donors.AsEnumerable();
            });
        }
    }
}