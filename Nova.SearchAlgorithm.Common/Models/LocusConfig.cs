using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Common.Models
{
    /// <summary>
    /// Central location from which locus-based functionality can be controlled.
    /// </summary>
    public static class LocusConfig
    {
        public static IEnumerable<Locus> AllLoci()
        {
            return Enum.GetValues(typeof(Locus)).Cast<Locus>();
        }
    }
}
