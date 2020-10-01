using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Utils.Extensions;

namespace Atlas.Common.Sql
{
    public static class SqlBuilderUtils
    {
        public static string ToInClause(this IEnumerable<string> values)
        {
            return $"({values.Select(x => $"'{x}'").StringJoin(",")})";
        }

        public static string ToInClause(this IEnumerable<int> values)
        {
            return $"({values.Select(x => x.ToString()).StringJoin(",")})";
        }

        public static string ToJoinBasedInClause(this IEnumerable<string> values, string joinType = "")
        {
            values = values.Select(x => $"'{x}'").ToList();
            return @$"{joinType} JOIN (
                SELECT {values.FirstOrDefault()} AS PGroupId
                {(values.Count() > 1 ? "UNION ALL SELECT" : "")} {string.Join(" UNION ALL SELECT ", values.Skip(1))}
            )";
        }

        public static string ToJoinBasedInClause(this IEnumerable<int> values, string joinType = "")
        {
            values = values.ToList();
            return @$"{joinType} JOIN (
                SELECT {values.FirstOrDefault()} AS PGroupId
                {(values.Count() > 1 ? "UNION ALL SELECT" : "")} {string.Join(" UNION ALL SELECT ", values.Skip(1))}
            )";
        }
    }
}