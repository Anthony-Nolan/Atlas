using System.Collections.Generic;

namespace Atlas.Client.Models.Search.Results
{
    public abstract class ResultSet<TResult> where TResult : Result
    {
        public int TotalResults { get; set; }
        public IEnumerable<TResult> Results { get; set; }
    }
}