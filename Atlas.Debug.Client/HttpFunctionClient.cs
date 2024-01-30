using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Atlas.Debug.Client.Models.HttpFunctions;
using Newtonsoft.Json;

namespace Atlas.Debug.Client
{
    /// <summary>
    /// Client to call HTTP-triggered functions.
    /// </summary>
    public interface IHttpFunctionClient
    {
        /// <summary>
        /// Post request to <paramref name="requestUri"/> with the given <paramref name="requestBody"/>.
        /// </summary>
        /// <exception cref="HttpFunctionException" />
        Task PostRequest<T>(string requestUri, T requestBody);

        /// <inheritdoc cref="PostRequest{T}"/>
        /// <returns>The deserialized response as type, TResponse.</returns>
        Task<TResponse> PostRequest<TBody, TResponse>(string requestUri, TBody requestBody);
    }

    /// <inheritdoc />
    public abstract class HttpFunctionClient : IHttpFunctionClient
    {
        private readonly HttpClient client;
        private readonly string apiKey;
        private readonly string baseUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpFunctionClient"/> class.
        /// </summary>
        /// <param name="client">The <see cref="HttpClient"/> instance.</param>
        /// <param name="apiKey">The API key.</param>
        protected HttpFunctionClient(HttpClient client, string apiKey)
        {
            this.client = client;
            this.apiKey = apiKey;
            baseUrl = client.BaseAddress?.ToString();
        }

        /// <inheritdoc />
        public async Task PostRequest<T>(string requestUri, T requestBody)
        {
            try
            {
                await SendRequestAndEnsureSuccess(HttpMethod.Post, requestUri, requestBody);
            }
            catch (Exception e)
            {
                throw WrappedException(requestUri, e);
            }
        }

        /// <inheritdoc />
        public async Task<TResponse> PostRequest<TBody, TResponse>(string requestUri, TBody requestBody)
        {
            try
            {
                var response = await SendRequestAndEnsureSuccess(HttpMethod.Post, requestUri, requestBody);
                return JsonConvert.DeserializeObject<TResponse>(await response.Content.ReadAsStringAsync());
            }
            catch (Exception e)
            {
                throw WrappedException(requestUri, e);
            }
        }

        private async Task<HttpResponseMessage> SendRequestAndEnsureSuccess<TBody>(HttpMethod method, string requestUri, TBody requestBody)
        {
            var request = BuildRequest(method, requestUri, requestBody);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return response;
        }

        private HttpRequestMessage BuildRequest<TBody>(HttpMethod method, string requestUri, TBody requestBody)
        {
            return new HttpRequestMessage(method, AppendRequestUriWithApiKey(requestUri))
            {
                Content = new StringContent(JsonConvert.SerializeObject(requestBody))
            };
        }

        private string AppendRequestUriWithApiKey(string requestUri)
        {
            var delimiter = requestUri.Contains("?") ? "&" : "?";
            return $"{requestUri}{delimiter}code={apiKey}";
        }

        private HttpFunctionException WrappedException(string requestUri, Exception ex) => new(baseUrl, requestUri, ex);
    }
}