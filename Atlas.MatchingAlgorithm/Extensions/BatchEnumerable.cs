using System.Collections.Generic;
using System.Linq;

namespace Atlas.MatchingAlgorithm.Extensions
{
    public static class BatchEnumerable
    {
        /// <summary>
        /// Splits an IEnumerable into multiple batches of the given batchSize, for any sort of processing
        /// which requires fixed size batches.
        /// </summary>
        /// <returns>
        /// An IEnumerable of IEnumerables, all of which have size batchSize except possibly the last.
        /// Between then they contain exactly the elemets of the original source IEnumerable.
        /// </returns>
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            T[] bucket = null;
            var count = 0;

            foreach (var item in source)
            {
                if (bucket == null)
                {
                    bucket = new T[batchSize];
                }

                bucket[count++] = item;

                if (count != batchSize)
                {
                    continue;
                }

                yield return bucket.Select(x => x);

                // Null in case there are no more items (see null check below)
                bucket = null;
                count = 0;
            }

            // Return the last bucket with all remaining elements
            if (bucket != null && count > 0)
            {
                yield return bucket.Take(count);
            }
        }
    }
}