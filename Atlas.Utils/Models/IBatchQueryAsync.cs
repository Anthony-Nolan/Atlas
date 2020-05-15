using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.Utils.Models
{
    public interface IBatchQueryAsync<T>
    {
        bool HasMoreResults { get; }
        Task<IEnumerable<T>> RequestNextAsync();
    }
}
