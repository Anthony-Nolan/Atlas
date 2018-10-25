using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Services.Matching
{
    /// <summary>
    /// Determines whether certain levels of filtering should be performed in the database layer or not
    /// Doing so affects performance positively in some cases, but negatively in others
    /// </summary>
    public interface IDatabaseFilteringAnalyser
    {
        bool ShouldFilterOnDonorTypeInDatabase(LocusSearchCriteria criteria);
        bool ShouldFilterOnRegistriesInDatabase(LocusSearchCriteria criteria);
    }
    
    // Implementations of these methods have been chosen based on a SQL database, tested against donor sets of 2 million and 8 million
    public class DatabaseFilteringAnalyser: IDatabaseFilteringAnalyser
    {
        public bool ShouldFilterOnDonorTypeInDatabase(LocusSearchCriteria criteria)
        {
            // There are significantly fewer cords than adults, so filtering out adults before p-group matching can make up for the cost of a JOIN to the donor table
            return criteria.SearchType == DonorType.Cord;
        }

        public bool ShouldFilterOnRegistriesInDatabase(LocusSearchCriteria criteria)
        {
            // Knowledge of the relative number of donors in each registry could help optimise this further.
            // Currently the two main search types are AN only, and aligned (UK) registries - in the former case, filtering in SQL speeds up search, in the latter it slows it down.
            return criteria.Registries.Count() < 4;
        }
    }
}