using System.Collections.Generic;

namespace Atlas.Utils.Core.Common
{
    public static class DictionaryExtensions
    {
        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            return dict.GetOrDefault(key, default(TValue));
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key,
            TValue defaultValue)
        {
            TValue ret;
            return dict.TryGetValue(key, out ret) ? ret : defaultValue;
        }
    }
}
