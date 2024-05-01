using Newtonsoft.Json;
using Polly;

namespace Atlas.ManualTesting.Common.Services
{
    public class AtlasHttpResult<TResult>
    {
        public bool WasSuccess => Result != null;
        public TResult? Result { get; set; }

        public AtlasHttpResult(TResult? result)
        {
            Result = result;
        }
    }

    public abstract class AtlasHttpRequester
    {
        private readonly HttpClient httpRequestClient;
        private readonly string requestUrl;

        protected AtlasHttpRequester(HttpClient httpRequestClient, string requestUrl)
        {
            this.httpRequestClient = httpRequestClient;
            this.requestUrl = requestUrl;
        }

        protected async Task<AtlasHttpResult<TResult>> PostRequest<TRequest, TResult>(TRequest request)
        {
            var retryPolicy = Policy.Handle<Exception>().RetryAsync(10);

            var requestResponse = await retryPolicy.ExecuteAndCaptureAsync(
                async () => await SendRequest<TRequest, TResult>(request));
            
            return new AtlasHttpResult<TResult>(requestResponse.Result);
        }

        private async Task<TResult?> SendRequest<TRequest, TResult>(TRequest request)
        {
            try
            {
                var response = await httpRequestClient.PostAsync(
                    requestUrl, new StringContent(JsonConvert.SerializeObject(request)));
                response.EnsureSuccessStatusCode();

                return JsonConvert.DeserializeObject<TResult>(await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send request {request}. Details: {ex.Message} " +
                                "Re-attempting until success or re-attempt count reached.");
                throw;
            }
        }
    }
}