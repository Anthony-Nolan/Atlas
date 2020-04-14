using System;

namespace Atlas.MatchingAlgorithm.Common.Models
{
    [Flags]
    public enum TypePosition
    {
        // Do not renumber, these values are stored in the database as integers.
        One = 1,
        Two = 2,
    }

    public static class TypePositionExtensions
    {
        public static TypePosition Other(this TypePosition typePosition)
        {
            switch (typePosition)
            {
                case TypePosition.Two:
                    return TypePosition.One;
                case TypePosition.One:
                    return TypePosition.Two;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typePosition), typePosition, null);
            }
        }
    }
}
