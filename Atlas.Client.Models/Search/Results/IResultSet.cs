using System.Collections.Generic;

namespace Atlas.Client.Models.Search.Results
{
    public interface IResultSet<TResult>
    {
        int TotalResults { get; set; }
        IEnumerable<TResult> Results { get; set; }
    }
}