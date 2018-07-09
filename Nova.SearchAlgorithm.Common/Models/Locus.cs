using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Common.Models
{
    public enum Locus
    {
        A,
        B,
        C,
        Dpb1,
        Dqb1,
        Drb1
    }

    public static class LocusHelpers
    {
        public static IEnumerable<Locus> AllLoci()
        {
            return Enum.GetValues(typeof(Locus)).Cast<Locus>();
        }
    }
}
