using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Atlas.Common.Utils.Extensions
{
    public static class StringExtensions
    {
        public static string ToMd5Hash(this string str)
        {
            if (str == null)
            {
                return "NULL";
            }

            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(str);
            var hash = md5.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static string StringJoin(this IEnumerable<string> strings, string separatorString)
        {
            return String.Join(separatorString, strings);
        }

        public static string StringJoin(this IEnumerable<string> strings, char separator)
        {
            return String.Join(separator, strings);
        }

        public static string StringJoinWithNewline(this IEnumerable<string> strings)
        {
            return String.Join(Environment.NewLine, strings);
        }
    }
}
