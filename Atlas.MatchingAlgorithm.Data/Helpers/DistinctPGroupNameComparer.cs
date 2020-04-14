using System.Collections.Generic;
using Atlas.MatchingAlgorithm.Data.Models;

namespace Atlas.MatchingAlgorithm.Data.Helpers
{
    internal class DistinctPGroupNameComparer : IEqualityComparer<PGroupName>
    {
        public bool Equals(PGroupName x, PGroupName y)
        {
            return x.Name == y.Name;
        }

        public int GetHashCode(PGroupName obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}