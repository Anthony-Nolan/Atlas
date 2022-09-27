using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.Common.Sql.BulkInsert
{
    /// <summary>
    /// Entity that can be used in Bulk Insert
    /// </summary>
    public interface IBulkInsertModel
    {
        public int Id { get; set; }
    }

    public static class BulkInsertModelExtensions
    {
        [Obsolete("Use Atlas.Common.Sql.BulkInsert.BulkInsertRepository instead")]
        public static IReadOnlyCollection<string> GetColumnNamesForBulkInsert<T>(this IEnumerable<T> model) where T : IBulkInsertModel
        {
            var columns = typeof(T).GetProperties().Select(p => p.Name).ToList();
            columns.Remove(nameof(IBulkInsertModel.Id));
            return columns;
        }
    }
}