using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.MatchingAlgorithm.Test.Performance.Models;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Test.Performance
{
    public static class SearchTimingService
    {
        public static async Task<SearchMetrics> TimeSearchRequest(SearchRequest request, AlgorithmInstanceInfo algorithmInstanceInfo)
        {
            var client = GetClient(algorithmInstanceInfo);
            var stopwatch = new Stopwatch();
            
            var httpRequest = GetSearchHttpRequest(request);

            var cts = new CancellationTokenSource();
            // Note that this does not stop the search on the server, so any subsequent searches will likely be slow 
            cts.CancelAfter(120000);
            try
            {
                stopwatch.Start();
                var result = await client.SendAsync(httpRequest, cts.Token);
                stopwatch.Stop();
                var content = await result.Content.ReadAsStringAsync();
                if (!result.IsSuccessStatusCode)
                {
                    throw new Exception($"Search request failed: {content}");
                }
            
                var deserialisedContent = JsonConvert.DeserializeObject<ResultSet<MatchingAlgorithmResult>>(content);

                return new SearchMetrics
                {
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    DonorsReturned = deserialisedContent.TotalResults,
                };
            }
            catch (OperationCanceledException)
            {
                return new SearchMetrics
                {
                    ElapsedMilliseconds = -1,
                    DonorsReturned = -1,
                };
            }
        }
        
        private static HttpClient GetClient(AlgorithmInstanceInfo algorithmInstanceInfo)
        {
            var httpClient = new HttpClient(new HttpClientHandler())
            {
                BaseAddress = new Uri(algorithmInstanceInfo.BaseUrl),
            };
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("X-Samples-ApiKey", algorithmInstanceInfo.Apikey);
            return httpClient;
        }

        private static HttpRequestMessage GetSearchHttpRequest(object body)
        {
            var serialisedBody = JsonConvert.SerializeObject(body);
            var bodyContent = new StringContent(serialisedBody, Encoding.UTF8, "application/json");
            return new HttpRequestMessage(HttpMethod.Post, "search") {Content = bodyContent};
        }
    }
}