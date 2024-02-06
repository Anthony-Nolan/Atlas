using System;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Debug.Client.Models.Exceptions;
using Newtonsoft.Json;

namespace Atlas.Debug.Client
{
    /// <summary>
    /// Client to call HTTP-triggered functions.
    /// </summary>
    public interface IHttpFunctionClient
    {
        /// <summary>
        /// Get request to <paramref name="requestUri"/>.
        /// </summary>
        /// <param name="requestUri"></param>
        Task<TResponse> GetRequest<TResponse>(string requestUri);

        /// <summary>
        /// Post request to <paramref name="requestUri"/> with the given <paramref name="requestBody"/>.
        /// </summary>
        /// <exception cref="HttpFunctionException" />
        Task PostRequest<TBody>(string requestUri, TBody requestBody);

        /// <inheritdoc cref="PostRequest{T}"/>
        /// <returns>The deserialized response as type, TResponse.</returns>
        Task<TResponse> PostRequest<TBody, TResponse>(string requestUri, TBody requestBody);
    }

    /// <inheritdoc />
    public abstract class HttpFunctionClient : IHttpFunctionClient
    {
        private readonly HttpClient client;
        private readonly string baseUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpFunctionClient"/> class.
        /// </summary>
        /// <param name="client">The <see cref="HttpClient"/> instance.</param>
        protected HttpFunctionClient(HttpClient client)
        {
            this.client = client;
            baseUrl = FormatBaseUrl(client.BaseAddress?.ToString());
        }

        /// <inheritdoc />
        public async Task<TResponse> GetRequest<TResponse>(string requestUri)
        {
            var response = await SendRequestAndEnsureSuccess(HttpMethod.Get, requestUri);
            return await DeserializeObject<TResponse>(response);
        }

        /// <inheritdoc />
        public async Task PostRequest<TBody>(string requestUri, TBody requestBody)
        {
            await SendRequestAndEnsureSuccess(HttpMethod.Post, requestUri, requestBody);
        }

        /// <inheritdoc />
        public async Task<TResponse> PostRequest<TBody, TResponse>(string requestUri, TBody requestBody)
        {
            var response = await SendRequestAndEnsureSuccess(HttpMethod.Post, requestUri, requestBody);
            return await DeserializeObject<TResponse>(response);
        }

        private static string FormatBaseUrl(string baseUrl) => baseUrl.EndsWith("/") ? baseUrl : $"{baseUrl}/";

        private async Task<HttpResponseMessage> SendRequestAndEnsureSuccess<TBody>(HttpMethod method, string requestUri, TBody requestBody)
        {
            try
            {
                var request = BuildRequest(method, requestUri, requestBody);
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (Exception e)
            {
                throw WrappedException(requestUri, e);
            }
        }

        /// <summary>
        /// Overload method to send a request without a request body.
        /// </summary>
        private async Task<HttpResponseMessage> SendRequestAndEnsureSuccess(HttpMethod method, string requestUri)
        {
            return await SendRequestAndEnsureSuccess<object>(method, requestUri, null);
        }

        private static HttpRequestMessage BuildRequest<TBody>(HttpMethod method, string requestUri, TBody requestBody)
        {
            var requestMessage = new HttpRequestMessage(method, requestUri);

            if (requestBody == null)
            {
                return requestMessage;
            }

            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(requestBody));
            return requestMessage;
        }

        private static async Task<TResponse> DeserializeObject<TResponse>(HttpResponseMessage response) =>
            JsonConvert.DeserializeObject<TResponse>(await response.Content.ReadAsStringAsync());

        private HttpFunctionException WrappedException(string requestUri, Exception ex) => new(baseUrl, requestUri, ex);
    }
}