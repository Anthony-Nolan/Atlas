using System;
using System.Security.Cryptography;
using System.Text;

namespace Atlas.Utils.Core.Common
{
    public static class StringExtensions
    {
        /// <summary>
        ///     Returns a string in camelCase.
        ///     Note: Update this method as necessary to convert from snake_case, kebab-case etc.
        /// </summary>
        /// <param name="str">Input string</param>
        /// <returns>Input string in camelCase</returns>
        public static string ToCamelCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }
            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        public static string ToMD5Hash(this string str)
        {
            if (str == null)
            {
                return null;
            }
            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(str);
                var hash = md5.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
