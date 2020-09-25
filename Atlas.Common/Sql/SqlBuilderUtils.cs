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
    }
}