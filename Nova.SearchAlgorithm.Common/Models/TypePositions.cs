using System;

namespace Nova.SearchAlgorithm.Common.Models
{
    [Flags]
    public enum TypePositions
    {
        // Do not renumber, these values are stored in the database as integers.
        One = 1,
        Two = 2,
    }

    public static class TypePositionsExtensions
    {
        public static TypePositions Other(this TypePositions typePositions)
        {
            switch (typePositions)
            {
                case TypePositions.Two:
                    return TypePositions.One;
                case TypePositions.One:
                    return TypePositions.Two;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typePositions), typePositions, null);
            }
        }
    }
}
