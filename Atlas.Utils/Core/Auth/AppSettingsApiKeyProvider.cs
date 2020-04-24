using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.Utils.Core.Auth
{
    public class AppSettingsApiKeyProvider : IApiKeyProvider
    {
        private const string KeyPrefix = "apiKey:";

        private readonly Dictionary<string, bool> keys;

        public AppSettingsApiKeyProvider()
        {
            var appSettings = ConfigurationManager.AppSettings;

            // Convert all appSettings which look like <add key="apiKey:[Some Key]" value="[true/false]" />
            keys = appSettings.AllKeys
                .Where(k => k.StartsWith(KeyPrefix))
                .ToDictionary(k => k.Substring(KeyPrefix.Length), k => bool.Parse(appSettings[k]));
        }

        public Task<bool> IsValid(string apiKey)
        {
            var ret = keys.ContainsKey(apiKey) && keys[apiKey];
            return Task.FromResult(ret);
        }
    }
}
