using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Extensions
{
    static class ExpandedHlaExtensions
    {
        public static IEnumerable<string> AllMatchingHlaNames(this ExpandedHla hla)
        {
            return hla.PGroups ?? Enumerable.Empty<string>();
        }
    }
}