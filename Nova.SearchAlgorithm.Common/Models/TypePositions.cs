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
}
