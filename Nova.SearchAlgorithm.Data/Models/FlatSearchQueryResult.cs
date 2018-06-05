using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Data.Models
{
    // TODO:NOVA-930 NOVA-1253 extend with more fields, matching the query in the sql matching repository,
    // in order to extract any further required information for matching and scoring.
    internal class FlatSearchQueryResult
    {
        public int DonorId { get; set; }
        public int TotalMatchCount { get; set; }


        public PotentialSearchResult ToPotentialSearchResult()
        {
            return new PotentialSearchResult
            {
                Donor = new DonorResult
                {
                    DonorId = DonorId
                },
                TotalMatchCount = TotalMatchCount
            };
        }
    }
}
