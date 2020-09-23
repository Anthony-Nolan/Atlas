using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;

namespace Atlas.MatchingAlgorithm.Services.Search.Matching
{
    /// <summary>
    /// Determines whether certain levels of filtering should be performed in the database layer or not
    /// Doing so affects performance positively in some cases, but negatively in others
    ///
    /// In all cases, the database level filtering must remove a large proportion of donors to be worth applying
    /// </summary>
    public interface IDatabaseFilteringAnalyser
    {
        bool ShouldFilterOnDonorTypeInDatabase(LocusSearchCriteria criteria);
    }
    
    // Implementations of these methods have been chosen based on a SQL database, tested against donor sets of 2 million and 8 million
    public class DatabaseFilteringAnalyser: IDatabaseFilteringAnalyser
    {
        public bool ShouldFilterOnDonorTypeInDatabase(LocusSearchCriteria criteria)
        {
            // There are significantly fewer cords than adults, so filtering out adults before p-group matching can make up for the cost of a JOIN to the donor table
            return criteria.SearchDonorType == DonorType.Cord;
        }
    }
}