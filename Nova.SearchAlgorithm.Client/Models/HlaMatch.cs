using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Client.Models
{
    [Flags]
    public enum TypePositions
    {
        None = 0,
        One = 1,
        Two = 2,
        Both = 3
    }

    public class HlaMatch
    {
        public string Locus { get; set; }
        public TypePositions SearchTypePosition { get; set; }
        public TypePositions MatchingTypePositions { get; set; }
        public string Name { get; set; }

        public int DonorId { get; set; }
    }
}
