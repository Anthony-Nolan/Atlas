using Atlas.MatchingAlgorithm.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Models
{
    public class MatchCriteria
    {
        public AlleleLevelMatchCriteria AlleleLevelMatchCriteria { set; get; }
        public NonHlaFilteringCriteria NonHlaFilteringCriteria { set; get; }
    }
}
