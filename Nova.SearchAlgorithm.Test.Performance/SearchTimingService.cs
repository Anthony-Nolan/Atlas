using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Test.Performance.Models;

namespace Nova.SearchAlgorithm.Test.Performance
{
    public static class SearchTimingService
    {
        public static async Task<SearchMetrics> TimeSearchRequest(SearchRequest request, AlgorithmInstanceInfo algorithmInstanceInfo)
        {
            var client = GetClient(algorithmInstanceInfo);
            var stopwatch = new Stopwatch();
            
            var httpRequest = GetSearchHttpRequest(request);

            stopwatch.Start();
            var result = await client.SendAsync(httpRequest);
            stopwatch.Stop();
            
            var content = await result.Content.ReadAsStringAsync();
            if (!result.IsSuccessStatusCode)
            {
                throw new Exception($"Search request failed: {content}");
            }
            
            var deserialisedContent = JsonConvert.DeserializeObject<SearchResultSet>(content);

            return new SearchMetrics
            {
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                DonorsReturned = deserialisedContent.TotalResults,
            };
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