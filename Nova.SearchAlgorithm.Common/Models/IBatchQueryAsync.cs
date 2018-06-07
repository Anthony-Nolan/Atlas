using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Common.Models
{
    public interface IBatchQueryAsync<T>
    {
        bool HasMoreResults { get; }
        Task<IEnumerable<T>> RequestNextAsync();
    }
}
