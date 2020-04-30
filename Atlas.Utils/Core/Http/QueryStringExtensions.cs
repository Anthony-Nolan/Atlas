using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Atlas.Utils.Core.Common;

namespace Atlas.Utils.Core.Http
{
    public static class QueryStringExtensions
    {
        private static ConcurrentDictionary<Type, List<PropertyInfo>> PropertyCache { get; } =
    new ConcurrentDictionary<Type, List<PropertyInfo>>();

        public static List<KeyValuePair<string, string>> ToQueryStringParams(this object obj)
        {
            var objType = obj.AssertArgumentNotNull(nameof(obj)).GetType();
            objType = Nullable.GetUnderlyingType(objType) ?? objType;
            if (obj is string || obj is IEnumerable || obj is DateTime || objType.IsPrimitive)
            {
                throw new ArgumentException("Can only convert a non-enumerable type with properties.");
            }

            var list = new List<KeyValuePair<string, string>>();
            AddPropToKvList(list, null, objType, obj);
            return list;
        }

        public static string ToQueryString(this IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var ret = string.Join("&", queryParams.Select(kv => $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}"));
            return ret.Length == 0 ? ret : "?" + ret;
        }

        private static void AddPropToKvList(ICollection<KeyValuePair<string, string>> list, string name, Type type,
            object value)
        {
            if (value == null)
            {
                return;
            }
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsPrimitive || (type == typeof(string)))
            {
                list.Add(new KeyValuePair<string, string>(name, Convert.ToString(value)));
            }
            else if (type == typeof(DateTime))
            {
                list.Add(new KeyValuePair<string, string>(name, ((DateTime)value).ToString("o")));
            }
            else if (type.IsEnum)
            {
                list.Add(new KeyValuePair<string, string>(name, Enum.GetName(type, value)));
            }
            else if (value is IEnumerable)
            {
                var index = 0;
                var elementType = type.IsGenericType
                    ? type.GetGenericArguments()[0]
                    : type.GetElementType();
                foreach (var item in (IEnumerable)value)
                {
                    AddPropToKvList(list, $"{name}[{index++}]", elementType, item);
                }
            }
            else
            {
                var props = PropertyCache.GetOrAdd(type, t => t
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.CanRead)
                    .ToList());
                props.ForEach(prop =>
                {
                    var propName = prop.Name.ToCamelCase();
                    var fullName = string.IsNullOrEmpty(name) ? propName : $"{name}.{propName}";
                    AddPropToKvList(list, fullName, prop.PropertyType, prop.GetValue(value));
                });
            }
        }
    }
}
