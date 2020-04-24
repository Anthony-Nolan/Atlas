using System;

namespace Nova.Utils.ApplicationInsights
{
    public static class IdGenerator
    {
        private static readonly Random Random = new Random();

        public static string NewId(string idName)
        {
            var idCode = GenerateIdCode();
            return $"{idName}-{idCode}";
        }

        private static string GenerateIdCode()
        {
            const int IdLength = 8;

            // We only use lowercase letters and digits excluding 0 and 1 to improve readability
            // (e.g. 0/O/o, 1/l/I/i/L)
            const string AllowedChars = "abcdefghijklmnopqrstuvwxyz23456789";
            var idChars = new char[IdLength];
            for (var i = 0; i < IdLength; i++)
            {
                idChars[i] = AllowedChars[Random.Next(AllowedChars.Length)];
            }
            return new string(idChars);
        }
    }
}
