using System.Linq;
using System.Net.Http;

namespace Nova.Utils.Middleware
{
    public static class IdentityExtensions
    {
        private const string FriendlyNameHeader = "X-Nova-Friendly-Name";
        private const string UsernameHeader = "X-Nova-Username";

        public static void SetFriendlyName(this HttpRequestMessage message, string friendlyName)
        {
            message.Headers.Add(FriendlyNameHeader, friendlyName);
        }

        public static string GetFriendlyName(this HttpRequestMessage message)
        {
            return message.Headers.GetValues(FriendlyNameHeader).Single();
        }

        public static void SetUsername(this HttpRequestMessage message, string username)
        {
            message.Headers.Add(UsernameHeader, username);
        }

        public static string GetUsername(this HttpRequestMessage message)
        {
            return message.Headers.GetValues(UsernameHeader).Single();
        }
    
    }
}