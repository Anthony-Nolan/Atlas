using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Data.Models;

namespace Nova.SearchAlgorithm.Repositories.Donors
{
    static class MatchingHlaExtensions
    {
        public static IEnumerable<string> AllMatchingHlaNames(this ExpandedHla hla)
        {
            return hla.PGroups ?? Enumerable.Empty<string>();
        }
    }
}