using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;

namespace Nova.SearchAlgorithm.Test.Performance
{
    internal class Program
    {

        public static async Task Main(string[] args)
        {
            var client = GetClient();

            var searchRequest = new SearchRequestBuilder()
                .WithTotalMismatchCount(0)
                .ForRegistries(new []{RegistryCode.AN, RegistryCode.DKMS, RegistryCode.NHSBT, RegistryCode.WBS, RegistryCode.FRANCE, RegistryCode.NMDP})
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0)
                .WithSearchHla(new PhenotypeInfo<string>
                {
                    A_1 = "24:02",
                    A_2 = "29:02",
                    B_1 = "45:01",
                    B_2 = "15:01",
                    C_1 = "03:03",
                    C_2 = "06:02",
                    Drb1_1 = "04:01",
                    Drb1_2 = "11:01",
                    Dqb1_1 = "03:01",
                    Dqb1_2 = "03:02"
                })
                .Build();
            
            var request = GetSearchRequest(searchRequest);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = await client.SendAsync(request);
            stopwatch.Stop();
            
            var content = await result.Content.ReadAsStringAsync();
        }

        private static HttpClient GetClient()
        {
            const string baseUrl = "http://localhost:30508";
            const string apikey = "test-key";
            
            var httpClient = new HttpClient(new HttpClientHandler())
            {
                BaseAddress = new Uri(baseUrl),
            };
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("X-Samples-ApiKey", apikey);
            return httpClient;
        }

        private static HttpRequestMessage GetSearchRequest(object body)
        {
            var serialisedBody = JsonConvert.SerializeObject(body);
            var bodyContent = new StringContent(serialisedBody, Encoding.UTF8, "application/json");
            
            var message = new HttpRequestMessage(HttpMethod.Post, "search");

            message.Content = bodyContent;
            return message;
        }
    }
}