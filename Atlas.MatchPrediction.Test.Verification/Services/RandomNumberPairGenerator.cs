using Atlas.Common.GeneticData;
using System;
using System.Collections.Generic;

namespace Atlas.MatchPrediction.Test.Verification.Services
{
    internal interface IRandomNumberPairGenerator
    {
        /// <returns>Pairs of numbers randomly selected between range of 0 to <see cref="maxPermittedValue"/></returns>
        IReadOnlyCollection<UnorderedPair<int>> GenerateRandomNumberPairs(int numberOfPairs, int maxPermittedValue);
    }
    
    internal class RandomNumberPairGenerator : IRandomNumberPairGenerator
    {
        private static readonly Random RandomNumberGenerator = new Random();

        public IReadOnlyCollection<UnorderedPair<int>> GenerateRandomNumberPairs(int numberOfPairs, int maxPermittedValue)
        {
            var randomNumberPairs = new List<UnorderedPair<int>>();
            for (var i = 0; i < numberOfPairs; i++)
            {
                randomNumberPairs.Add(new UnorderedPair<int>(
                    RandomNumberGenerator.Next(maxPermittedValue),
                    RandomNumberGenerator.Next(maxPermittedValue)));
            }

            return randomNumberPairs;
        }
    }
}
