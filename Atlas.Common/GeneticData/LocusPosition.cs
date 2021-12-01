using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.Common.GeneticData
{
    /// <summary>
    /// Position of an allele or other information *within* a locus.
    /// Represents the biological phase, i.e. which chromosome an allele is on.
    /// Biological phase is *not* consistent across loci - i.e. A1 is not guaranteed to be the same phase as B1
    /// All that is implied is that an allele at A1 has different phase to that at A2 
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LocusPosition
    {
        One,
        Two
    }
    
    public static class LocusPositionExtensions
    {
        public static LocusPosition Other(this LocusPosition typePosition)
        {
            return typePosition switch
            {
                LocusPosition.Two => LocusPosition.One,
                LocusPosition.One => LocusPosition.Two,
                _ => throw new ArgumentOutOfRangeException(nameof(typePosition), typePosition, null)
            };
        }
    }
}