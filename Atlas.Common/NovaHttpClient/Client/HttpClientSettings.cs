using System;
using Newtonsoft.Json;

namespace Atlas.Common.NovaHttpClient
{
    public class HttpClientSettings
    {
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
        public string ClientName { get; set; }
        public TimeSpan? Timeout { get; set; }
        public JsonSerializerSettings JsonSettings { get; set; }
    }
}