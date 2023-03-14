using Newtonsoft.Json;

namespace Atlas.Client.Models.Search.Results.ResultSet
{
    public abstract class BatchedResultSet<TResult> : ResultSet<TResult> where TResult : Result
    {
        public override bool ShouldSerializeResults() => !BatchedResult;

        [JsonIgnore]
        public bool BatchedResult { get; set; } = true;
    }
}
