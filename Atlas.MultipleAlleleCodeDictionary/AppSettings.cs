using Microsoft.Extensions.Configuration;

namespace Atlas.MultipleAlleleCodeDictionary
{
    public class AppSettings
    {
        public string StorageConnectionString { get; set; }
        public string TableName { get; set; }

        public static AppSettings LoadAppSettings()
        {
            var configRoot = new ConfigurationBuilder()
                .AddJsonFile("settings.json")
                .Build();
            var appSettings = configRoot.Get<AppSettings>();
            return appSettings;
        }
    }
}