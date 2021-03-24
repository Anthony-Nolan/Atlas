using Atlas.Client.Models.Search.Results.Matching;

namespace Atlas.Client.Models.Search.Results
{
    public interface IResult
    {
        string DonorCode { get; set; }
        ScoringResult ScoringResult { get; }
    }
}