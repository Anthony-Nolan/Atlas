using System;
using System.Collections.Generic;
using Atlas.Common.GeneticData;

namespace Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation
{
    internal interface IRandomNumberGenerator
    {
        /// <returns>Collection of number pairs: each member of the pair is randomly selected between requested range.</returns>
        IReadOnlyCollection<UnorderedPair<int>> GenerateRandomNumberPairs(GenerateRandomNumberRequest request);
    }

    internal class RandomNumberGenerator : IRandomNumberGenerator
    {
        private static readonly Random Generator = new Random();

        public IReadOnlyCollection<UnorderedPair<int>> GenerateRandomNumberPairs(GenerateRandomNumberRequest request)
        {
            var randomNumberPairs = new List<UnorderedPair<int>>();
            for (var i = 0; i < request.Count; i++)
            {
                randomNumberPairs.Add(new UnorderedPair<int>(
                    Generator.Next(request.MinPermittedValue, request.MaxPermittedValue),
                    Generator.Next(request.MinPermittedValue, request.MaxPermittedValue)));
            }

            return randomNumberPairs;
        }
    }

    internal class GenerateRandomNumberRequest
    {
        public int Count { get; set; }

        /// <summary>
        /// Defaults to 0
        /// </summary>
        public int MinPermittedValue { get; set; }

        public int MaxPermittedValue { get; set; }
    }
}
