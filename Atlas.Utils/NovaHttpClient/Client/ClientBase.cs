using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Atlas.Utils.Core.ApplicationInsights;
using Atlas.Utils.Core.Http;
using Atlas.Utils.Core.Middleware.RequestId;
using Atlas.Utils.NovaHttpClient.ApplicationInsights;
using Atlas.Utils.NovaHttpClient.Http;
using Atlas.Utils.NovaHttpClient.RequestId;

namespace Atlas.Utils.NovaHttpClient.Client
{
    public abstract class ClientBase
    {
        private const int DefaultTimeoutSeconds = 100;

        /*
         * We maintain a cache of underlying HTTP clients, so that clients with the same name will share
         * an underlying HTTP client. This prevents us saturating the available sockets by creating arbitrary
         * numbers of HTTP clients
         */
        private static readonly Dictionary<string, HttpClient> ClientCache = new Dictionary<string, HttpClient>();
        private readonly JsonMediaTypeFormatter formatter;
        private readonly HttpClient client;
        private readonly string clientName;
        private readonly HttpErrorParser errorsParser;
        private readonly ILogger logger;

        protected ClientBase(HttpClientSettings settings, ILogger logger = null, HttpMessageHandler handler = null, HttpErrorParser errorsParser = null)
        {
            clientName = ServiceNameValidator.Validate(settings.ClientName);
            client = GetClient(
                settings.BaseUrl,
                settings.ApiKey,
                settings.Timeout ?? TimeSpan.FromSeconds(DefaultTimeoutSeconds),
                handler);

            formatter = new JsonMediaTypeFormatter
            {
                SerializerSettings = settings.JsonSettings
            };
            this.logger = logger ?? new NoOpLogger();

            this.errorsParser = errorsParser ?? new HttpErrorParser();
        }

        private string ApiKeyHeaderKey => ApiKeyConstants.HeaderKey;
        private string LegacyApiKeyHeaderKey => ApiKeyConstants.LegacyHeaderKey;

        protected virtual HttpRequestMessage GetRequest(HttpMethod method, string pathname,
            List<KeyValuePair<string, string>> parameters = null, object body = null)
        {
            var message = new HttpRequestMessage(method, RequestUri(pathname, parameters));
            AddMessageBody(message, body);
            AddRequestId(message);
            return message;
        }

        protected async Task<T> MakeRequestAsync<T>(HttpRequestMessage request)
        {
            var response = await SendRequestAsync(request);
            await AssertResponseOk(response);
            return await ContentAsAsync<T>(response.Content);
        }

        private async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
        {
            var requestEvent = RequestEvent.OutgoingNovaRequest(request);
            try
            {
                var response = await client.SendAsync(request);
                requestEvent.RequestCompleted((int)response.StatusCode);
                return response;
            }
            catch (HttpRequestException e)
            {
                requestEvent.NoResponseReceived();
                throw new AtlasHttpException(HttpStatusCode.ServiceUnavailable, clientName + " service unavailable.", e);
            }
            finally
            {
                logger.SendEvent(requestEvent);
            }
        }

        private async Task<T> ContentAsAsync<T>(HttpContent content)
        {
            // There is no way (that I could find) to overload methods by type parameter if it is not a parameter type
            // This allows different parameters to be treated differently at run time
            if (typeof(T) == typeof(string))
            {
                // This double cast is required because C# forbids arbitrary casting, but allows casting
                // from anything to object and from object to anything
                return (T)(object)await content.ReadAsStringAsync();
            }

            if (typeof(T) == typeof(byte[]))
            {
                // This double cast is required because C# forbids arbitrary casting, but allows casting
                // from anything to object and from object to anything
                return (T)(object)await content.ReadAsByteArrayAsync();
            }

            if (typeof(T) == typeof(HttpContentModel))
            {
                // This double cast is required because C# forbids arbitrary casting, but allows casting
                // from anything to object and from object to anything
                return (T)(object)new HttpContentModel
                {
                    Content = await content.ReadAsStringAsync(),
                    ContentType = content.Headers.ContentType.MediaType
                };
            }

            return await content.ReadAsAsync<T>();
        }

        private async Task AssertResponseOk(HttpResponseMessage response)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.Created:
                case HttpStatusCode.NoContent:
                    return;
                case HttpStatusCode.NotFound:
                    await errorsParser.ThrowNotFoundException(response.Content);
                    break;
                case HttpStatusCode.BadRequest:
                    await errorsParser.ThrowBadRequestException(response.Content);
                    break;
                default:
                    await errorsParser.ThrowGenericException(response.StatusCode, response.Content);
                    break;
            }
        }

        private static string RequestUri(string pathname, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            return pathname + parameters?.ToQueryString();
        }

        private void AddMessageBody(HttpRequestMessage message, object body)
        {
            if (body != null)
            {
                message.Content = new ObjectContent(body.GetType(), body, formatter);
            }
        }

        private void AddRequestId(HttpRequestMessage message)
        {
            var newRequestIdSegment = RequestIdSegmentGenerator.NewSegment(clientName);
            var idString = message.GetRequestId()?.AppendRequestIdSegment(newRequestIdSegment);

            message.SetRequestId(idString);
        }

        private HttpClient GetClient(string baseUrl, string apikey, TimeSpan timeout, HttpMessageHandler handler = null)
        {
            if (handler != null)
            {
                // We cannot use the cached client, since the handler may be different.
                return CreateNewClient(baseUrl, apikey, timeout, handler);
            }

            if (ClientCache.ContainsKey(clientName))
            {
                return ClientCache[clientName];
            }

            var newClient = CreateNewClient(baseUrl, apikey, timeout);
            ClientCache[clientName] = newClient;
            return newClient;
        }

        private HttpClient CreateNewClient(string baseUrl, string apikey, TimeSpan timeout,
            HttpMessageHandler handler = null)
        {
            if (ClientCache.ContainsKey(baseUrl))
            {
                return ClientCache[baseUrl];
            }

            var newClient = new HttpClient(handler ?? new HttpClientHandler())
            {
                BaseAddress = new Uri(baseUrl)
            };
            newClient.Timeout = timeout;
            newClient.DefaultRequestHeaders.Accept.Clear();
            newClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            newClient.DefaultRequestHeaders.Add(ApiKeyHeaderKey, apikey);
            newClient.DefaultRequestHeaders.Add(LegacyApiKeyHeaderKey, apikey);
            return newClient;
        }
    }
}