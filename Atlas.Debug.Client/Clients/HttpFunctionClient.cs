using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Debug.Client.Models.Exceptions;
using Atlas.Debug.Client.Models.Validation;
using Newtonsoft.Json;

namespace Atlas.Debug.Client.Clients
{
    /// <summary>
    /// Base class for clients that call HTTP-triggered Atlas functions.
    /// </summary>
    public abstract class HttpFunctionClient : ICommonAtlasFunctions
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

        /// <summary>
        /// Get request to <paramref name="requestUri"/>.
        /// </summary>
        /// <param name="requestUri"></param>
        public async Task<TResponse> GetRequest<TResponse>(string requestUri)
        {
            var response = await SendRequestAndEnsureSuccess(HttpMethod.Get, requestUri);
            return await DeserializeResponseContent<TResponse>(response.Content);
        }

        /// <summary>
        /// Post request to <paramref name="requestUri"/> with the given <paramref name="requestBody"/>.
        /// </summary>
        /// <exception cref="HttpFunctionException" />
        public async Task PostRequest<TBody>(string requestUri, TBody requestBody)
        {
            await SendRequestAndEnsureSuccess(HttpMethod.Post, requestUri, requestBody);
        }

        /// <inheritdoc cref="PostRequest{T}"/>
        /// <returns>The deserialized response as type, TResponse.</returns>
        public async Task<TResponse> PostRequest<TBody, TResponse>(string requestUri, TBody requestBody)
        {
            var response = await SendRequestAndEnsureSuccess(HttpMethod.Post, requestUri, requestBody);
            return await DeserializeResponseContent<TResponse>(response.Content);
        }

        /// <inheritdoc cref="PostRequest{T}"/>
        /// <returns>
        /// The deserialized response as type, TResponse, if the request was successful.
        /// Or the deserialized collection of validation failures in the case of a bad request.
        /// </returns>
        public async Task<ResponseFromValidatedRequest<TResponse>> PostValidatedRequest<TBody, TResponse>(string requestUri, TBody requestBody)
        {
            try
            {
                var response = await SendRequestAndEnsureSuccess(HttpMethod.Post, requestUri, requestBody);
                var successfulResult = await DeserializeResponseContent<TResponse>(response.Content);
                return new ResponseFromValidatedRequest<TResponse>(successfulResult);
            }
            catch (HttpFunctionException e) when (e.HttpStatusCode == HttpStatusCode.BadRequest)
            {
                var failures = await DeserializeResponseContent<List<RequestValidationFailure>>(e.ResponseContent);
                return new ResponseFromValidatedRequest<TResponse>(failures);
            }
            // any other kind of exception is unexpected and should be thrown.
        }

        /// <inheritdoc />
        public async Task<string> HealthCheck()
        {
            return await GetRequest<string>("HealthCheck");
        }

        private static string FormatBaseUrl(string baseUrl) => baseUrl.EndsWith("/") ? baseUrl : $"{baseUrl}/";

        private async Task<HttpResponseMessage> SendRequestAndEnsureSuccess<TBody>(HttpMethod method, string requestUri, TBody requestBody)
        {
            // default to a bad request response in case something goes wrong with building the request before its even sent
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);

            try
            {
                var request = BuildRequest(method, requestUri, requestBody);
                response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (Exception e)
            {
                throw new HttpFunctionException(response.StatusCode, response.Content, baseUrl, requestUri, e);
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

        private static async Task<TResponse> DeserializeResponseContent<TResponse>(HttpContent responseContent) =>
            JsonConvert.DeserializeObject<TResponse>(await responseContent.ReadAsStringAsync());
    }
}