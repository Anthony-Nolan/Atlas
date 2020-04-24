using System.Collections.Generic;

namespace Atlas.Utils.Core.Helpers
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Attempts to add the specified key and value to the dictionary.
        /// </summary>
        /// <returns>False if key is null or already exists in dictionary, else true.</returns>
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (key == null || dictionary.ContainsKey(key))
            {
                return false;
            }

            dictionary.Add(key, value);
            return true;
        }
    }
}
