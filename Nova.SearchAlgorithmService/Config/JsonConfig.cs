using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Nova.SearchAlgorithmService.Config
{
    public static class JsonConfig
    {
        static JsonConfig()
        {
            GlobalSettings = new JsonSerializerSettings
            {
                // We want to stick with Javascript conventions on client-side, so use camel-cased names
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DateParseHandling = DateParseHandling.DateTime,
                NullValueHandling = NullValueHandling.Ignore
            };
            GlobalSettings.Converters.Add(new StringEnumConverter());
        }

        public static JsonSerializerSettings GlobalSettings { get; }
    }
}