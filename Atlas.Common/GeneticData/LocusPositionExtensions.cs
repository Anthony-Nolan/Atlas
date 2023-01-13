using System;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.Common.GeneticData
{
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