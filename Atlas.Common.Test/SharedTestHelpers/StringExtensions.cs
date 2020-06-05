using System.IO;
using System.Text;

namespace Atlas.Common.Test.SharedTestHelpers
{
    public static class StringExtensions
    {
        public static Stream ToStream(this string s)
        {
            return new MemoryStream(Encoding.Default.GetBytes(s));
        }
    }
}