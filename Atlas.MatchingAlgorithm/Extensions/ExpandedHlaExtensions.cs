using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Common.Models;

namespace Atlas.MatchingAlgorithm.Extensions
{
    static class ExpandedHlaExtensions
    {
        public static IEnumerable<string> AllMatchingHlaNames(this ExpandedHla hla)
        {
            return hla.PGroups ?? Enumerable.Empty<string>();
        }
    }
}