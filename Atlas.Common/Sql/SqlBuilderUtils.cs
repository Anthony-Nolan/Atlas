using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Utils.Extensions;

namespace Atlas.Common.Sql
{
    public static class SqlBuilderUtils
    {
        public static string ToInClause(this IEnumerable<string> values)
        {
            return $"({values.StringJoin(",")})";
        }

        public static string ToInClause<T>(this IEnumerable<T> values)
        {
            return values.Select(v => v.ToString()).ToInClause();
        }

        public static string ToJoinBasedInClause(this IEnumerable<string> values, string joinType = "")
        {
            values = values.ToList();
            return @$"{joinType} JOIN (
                SELECT {values.FirstOrDefault()} AS PGroupId
                {(values.Count() > 1 ? "UNION ALL SELECT" : "")} {string.Join(" UNION ALL SELECT ", values.Skip(1))}
            )";
        }

        public static string ToJoinBasedInClause<T>(this IEnumerable<T> values, string joinType = "")
        {
            return values.Select(v => v.ToString()).ToJoinBasedInClause();
        }
    }
}