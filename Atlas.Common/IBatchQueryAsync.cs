using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.Common
{
    public interface IBatchQueryAsync<T>
    {
        bool HasMoreResults { get; }
        Task<IEnumerable<T>> RequestNextAsync();
    }
}
