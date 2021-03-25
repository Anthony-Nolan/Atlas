using Atlas.Client.Models.Search.Results.Matching;

namespace Atlas.Client.Models.Search.Results
{
    public abstract class Result
    {
        /// <summary>
        /// The external Donor Code (possibly referred to as an ID) of the donor.
        /// This will match the id for a donor provided by a consumer at the time of donor import.
        /// </summary>
        public string DonorCode { get; set; }

        public virtual ScoringResult ScoringResult { get; set; }
    }
}