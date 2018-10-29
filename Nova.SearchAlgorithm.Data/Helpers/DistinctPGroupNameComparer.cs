using System.Collections.Generic;
using Nova.SearchAlgorithm.Data.Entity;

namespace Nova.SearchAlgorithm.Data.Helpers
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