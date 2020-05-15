using System;
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
                return null;
            }

            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(str);
            var hash = md5.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
