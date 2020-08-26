using System.Collections.Generic;
using System.Linq;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models
{
    internal interface IModel
    {
        public int Id { get; set; }
    }

    internal static class ModelExtensions
    {
        public static IReadOnlyCollection<string> GetColumnNamesForBulkInsert<T>(this IEnumerable<T> model) where T : IModel
        {
            var columns = typeof(T).GetProperties().Select(p => p.Name).ToList();
            columns.Remove(nameof(IModel.Id));
            return columns;
        }
    }
}
