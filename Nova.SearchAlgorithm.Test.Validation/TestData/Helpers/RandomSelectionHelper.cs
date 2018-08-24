using System;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Helpers
{
    public static class RandomSelectionHelper
    {
        // ReSharper disable once InconsistentNaming
        private static readonly Random random = new Random();

        public static T GetRandomElement<T>(this IReadOnlyList<T> data)
        {
            return data[random.Next(data.Count)];
        }
    }
}