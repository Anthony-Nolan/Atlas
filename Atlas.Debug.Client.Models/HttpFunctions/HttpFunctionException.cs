using System;

namespace Atlas.Debug.Client.Models.HttpFunctions
{
    /// <summary>
    /// Represents an exception that occurs when invoking a http-triggered function.
    /// </summary>
    public class HttpFunctionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpFunctionException"/> class with the specified base URL, request URI, and inner exception.
        /// </summary>
        /// <param name="baseUrl">The base URL of the debug endpoint.</param>
        /// <param name="requestUri">The request URI of the debug endpoint.</param>
        /// <param name="innerException">The inner exception that caused this exception.</param>
        public HttpFunctionException(string baseUrl, string requestUri, Exception innerException) :
            base(BuildMessage(baseUrl, requestUri), innerException)
        {
        }

        private static string BuildMessage(string baseUrl, string requestUri) =>
            $"Error when invoking http-triggered function, baseUrl: {baseUrl}, requestUri: {requestUri}";
    }
}