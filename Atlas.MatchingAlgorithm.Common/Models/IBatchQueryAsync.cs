using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Common.Models
{
    public interface IBatchQueryAsync<T>
    {
        bool HasMoreResults { get; }
        Task<IEnumerable<T>> RequestNextAsync();
    }
}
