using System;

namespace Atlas.Debug.Client.Models.Settings
{
    /// <summary>
    /// Settings for calling HTTP-triggered functions.
    /// </summary>
    public abstract class HttpFunctionSettings
    {
        /// <summary>
        /// Base URL for the HTTP-triggered function, i.e., the functions app URL.
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// API key for the HTTP-triggered function.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Optional timeout for the HTTP request.
        /// </summary>
        public TimeSpan? RequestTimeOut { get; set; }
    }
}