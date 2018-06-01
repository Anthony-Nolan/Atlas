using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Entity;

namespace Nova.SearchAlgorithm.Data.Models
{
    internal class FlatSearchQueryResult
    {
        public int DonorId { get; set; }
        public int TotalMatchCount { get; set; }


        public PotentialSearchResult ToPotentialSearchResult()
        {
            return new PotentialSearchResult
            {
                Donor = new DonorResult
                {
                    DonorId = DonorId
                },
                TotalMatchCount = TotalMatchCount
            };
        }
    }
}
