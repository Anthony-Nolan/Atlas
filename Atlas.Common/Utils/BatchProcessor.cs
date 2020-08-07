using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MoreLinq;

namespace Atlas.Common.Utils
{
    public static class BatchProcessor
    {
        public static async  Task<IEnumerable<TResult>> ProcessInBatchesAsync<TInput, TResult>(
            this IEnumerable<TInput> inputs,
            int batchSize,
            Func<IEnumerable<TInput>, Task<IEnumerable<TResult>>> processBatch)
        {
            var results = new List<TResult>();
            foreach (var batch in inputs.Batch(batchSize))
            {
                var batchResults = await processBatch(batch);
                results.AddRange(batchResults);
            }

            return results;
        }
    }
}