using Atlas.Debug.Client.Models.Exceptions;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Clients.Scoring
{
    public abstract class MatchingAlgorithmHttpFunctionClient
    {
        private readonly HttpClient client;
        private readonly string baseUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="MatchingAlgorithmHttpFunctionClient"/> class.
        /// </summary>
        /// <param name="client">The <see cref="HttpClient"/> instance.</param>
        protected MatchingAlgorithmHttpFunctionClient(HttpClient client)
        {
            this.client = client;
            baseUrl = FormatBaseUrl(client.BaseAddress?.ToString());
        }

        public async Task<TResponse> PostRequest<TBody, TResponse>(string requestUri, TBody requestBody)
        {
            var response = await SendRequestAndEnsureSuccess(HttpMethod.Post, requestUri, requestBody);
            return await DeserializeResponseContent<TResponse>(response.Content);
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
