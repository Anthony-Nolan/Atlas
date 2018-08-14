using System;

namespace Nova.SearchAlgorithm.Common.Models
{
    [Flags]
    public enum TypePositions
    {
        // Do not renumber, these values are stored in the database as integers.
        None = 0,
        One = 1,
        Two = 2,
        Both = 3
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
                case TypePositions.None:
                case TypePositions.Both:
                    throw new Exception("Can only get other position for a single type position");
                default:
                    throw new ArgumentOutOfRangeException(nameof(typePositions), typePositions, null);
            }
        }
    }
}
