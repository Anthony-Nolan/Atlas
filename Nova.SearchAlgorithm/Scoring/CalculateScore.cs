using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Scoring
{
    public interface ICalculateScore
    {
        Task<PotentialMatch> Score(PotentialMatch potentialMatch);
    }

    public class CalculateScore : ICalculateScore
    {
        // TODO:NOVA-1170 inject dependencies
        public CalculateScore()
        {
        }

        public Task<PotentialMatch> Score(PotentialMatch potentialMatch)
        {
            // TODO:NOVA-1170 (write tests and) implement
            return Task.FromResult(potentialMatch);
        }
    }
}