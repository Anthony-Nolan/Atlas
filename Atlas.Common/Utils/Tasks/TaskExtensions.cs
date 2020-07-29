using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.Common.Utils.Tasks
{
    public static class TaskExtensions
    {
        /// <summary>
        /// awaits each of a set of tasks in turn, and returns a collection of the results.
        /// Can be used in place of Task.WhenAll() when we explicitly want to run the awaited processes in series, rather than in parallel 
        /// </summary>
        public static async Task<IEnumerable<T>> WhenEach<T>(IEnumerable<Task<T>> tasks)
        {
            var results = new List<T>();
            foreach (var task in tasks)
            {
                results.Add(await task);
            }

            return results;
        }
    }
}