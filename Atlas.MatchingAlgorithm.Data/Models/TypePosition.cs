using System;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Data.Models
{
    /// <summary>
    /// Internal position enum allows the data project to ensure that the stored backing values are never changed. 
    /// </summary>
    internal enum TypePosition
    {
        // Do not renumber, these values are stored in the database as integers.
        One = 1,
        Two = 2,
    }

    internal static class TypePositionExtensions
    {
        public static LocusPosition ToLocusPosition(this TypePosition typePosition)
        {
            return typePosition switch
            {
                TypePosition.One => LocusPosition.Position1,
                TypePosition.Two => LocusPosition.Position2,
                _ => throw new ArgumentOutOfRangeException(nameof(typePosition), typePosition, null)
            };
        }

        public static TypePosition ToTypePosition(this LocusPosition locusPosition)
        {
            return locusPosition switch
            {
                LocusPosition.Position1 => TypePosition.One,
                LocusPosition.Position2 => TypePosition.Two,
                _ => throw new ArgumentOutOfRangeException(nameof(locusPosition), locusPosition, null)
            };
        }
    }
}