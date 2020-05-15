using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Atlas.Common.NovaHttpClient.Http
{
    public static class QueryStringExtensions
    {
        private static ConcurrentDictionary<Type, List<PropertyInfo>> PropertyCache { get; } = new ConcurrentDictionary<Type, List<PropertyInfo>>();

        public static string ToQueryString(this IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var ret = string.Join("&", queryParams.Select(kv => $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}"));
            return ret.Length == 0 ? ret : "?" + ret;
        }
    }
}