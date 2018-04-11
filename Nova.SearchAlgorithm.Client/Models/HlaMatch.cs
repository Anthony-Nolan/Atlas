using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Client.Models
{
    public class HlaMatch
    {
        public string Locus { get; set; }
        public int SearchTypePosition { get; set; }
        public int MatchingTypePosition { get; set; }
        public string Name { get; set; }

        public int DonorId { get; set; }
    }
}
