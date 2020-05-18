using System;

namespace Atlas.Common.GeneticData.PhenotypeInfo
{
    /// <summary>
    /// Position of an allele or other information *within* a locus.
    /// Represents the biological phase, i.e. which chromosome an allele is on.
    /// Biological phase is *not* consistent across loci - i.e. A1 is not guaranteed to be the same phase as B1
    /// All that is implied is that an allele at A1 has different phase to that at A2 
    /// </summary>
    public enum LocusPosition
    {
        Position1,
        Position2
    }
    
    public static class LocusPositionExtensions
    {
        public static LocusPosition Other(this LocusPosition typePosition)
        {
            return typePosition switch
            {
                LocusPosition.Position2 => LocusPosition.Position1,
                LocusPosition.Position1 => LocusPosition.Position2,
                _ => throw new ArgumentOutOfRangeException(nameof(typePosition), typePosition, null)
            };
        }
    }
}