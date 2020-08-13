using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.Common.Helpers
{
    public static class RandomSelectionHelper
    {
        // ReSharper disable once InconsistentNaming
        private static readonly Random random = new Random();

        public static T GetRandomElement<T>(this IReadOnlyList<T> data)
        {
            return data[random.Next(data.Count)];
        }

        public static IEnumerable<T> GetRandomSelection<T>(this IList<T> data, int min, int max)
        {
            var randomMax = Math.Min(max, data.Count);
            return data.Shuffle().Take(random.Next(min, randomMax));
        }

        // Fisher-Yates shuffle 
        // https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
        public static IList<T> Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = random.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }

            return list;
        }
    }
}