using System;
using System.Net;
using System.Net.Http;

namespace Atlas.Debug.Client.Models.Exceptions
{
    /// <summary>
    /// Represents an exception that occurs when invoking a http-triggered function.
    /// </summary>
    public class HttpFunctionException : Exception
    {
        /// <summary>
        /// Status code of the response that threw this exception.
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; set; }
        
        /// <summary>
        /// Content of the response that threw this exception.
        /// </summary>
        public HttpContent ResponseContent { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpFunctionException"/> class with the specified base URL, request URI, and inner exception.
        /// </summary>
        /// <param name="statusCode">The status code of the response that threw this exception.</param>
        /// <param name="responseContent">The content of the response that threw this exception.</param>
        /// <param name="baseUrl">The base URL of the debug endpoint.</param>
        /// <param name="requestUri">The request URI of the debug endpoint.</param>
        /// <param name="innerException">The inner exception that caused this exception.</param>
        public HttpFunctionException(
            HttpStatusCode statusCode, 
            HttpContent responseContent, 
            string baseUrl, 
            string requestUri, 
            Exception innerException) : base(BuildMessage(baseUrl, requestUri), innerException)
        {
            HttpStatusCode = statusCode;
            ResponseContent = responseContent;
        }

        private static string BuildMessage(string baseUrl, string requestUri) =>
            $"Error when invoking http-triggered function, baseUrl: {baseUrl}, requestUri: {requestUri}";
    }
}