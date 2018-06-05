using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Data.Models;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Scoring
{
    public interface ICalculateScore
    {
        Task<PotentialSearchResult> Score(DonorMatchCriteria searchCriteria, PotentialSearchResult potentialMatch);
    }

    public class CalculateScore : ICalculateScore
    {
        // TODO:NOVA-930 inject dependencies
        public CalculateScore()
        {
        }

        public Task<PotentialSearchResult> Score(DonorMatchCriteria searchCriteria, PotentialSearchResult potentialMatch)
        {
            // TODO:NOVA-930 (write tests and) implement
            return Task.FromResult(potentialMatch);
        }
    }
}