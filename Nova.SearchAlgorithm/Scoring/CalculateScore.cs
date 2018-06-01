using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Scoring
{
    public interface ICalculateScore
    {
        Task<PotentialSearchResult> Score(PotentialSearchResult potentialMatch);
    }

    public class CalculateScore : ICalculateScore
    {
        // TODO:NOVA-930 inject dependencies
        public CalculateScore()
        {
        }

        public Task<PotentialSearchResult> Score(PotentialSearchResult potentialMatch)
        {
            // TODO:NOVA-930 (write tests and) implement
            return Task.FromResult(potentialMatch);
        }
    }
}