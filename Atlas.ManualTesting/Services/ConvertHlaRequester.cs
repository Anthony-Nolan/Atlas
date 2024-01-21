using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.ManualTesting.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Atlas.ManualTesting.Services
{
    /// <summary>
    /// Copied <see cref="Atlas.MatchingAlgorithm.Functions.Models.Debug.HlaConversionRequest"/>,
    /// instead of moving the model to the MatchingAlgorithm.Client.Models project,
    /// to avoid breaking client/interface changes that would come from moving <see cref="Atlas.HlaMetadataDictionary.ExternalInterface.Models.TargetHlaCategory"/>.
    /// </summary>
    public class ConvertHlaRequest
    {
        public Locus Locus { get; set; }
        public string HlaName { get; set; }
        public TargetHlaCategory TargetHlaCategory { get; set; }

        public override string ToString()
        {
            return $"convert {Locus},{HlaName} to {TargetHlaCategory}";
        }
    }

    public interface IConvertHlaRequester
    {
        Task<IEnumerable<string>> ConvertHla(ConvertHlaRequest request);
    }

    internal class ConvertHlaRequester : IConvertHlaRequester
    {
        private const string FailedRequestPrefix = "Failed to";
        private static readonly HttpClient HttpRequestClient = new();
        private readonly HlaMetadataDictionarySettings hlaMetadataDictionarySettings;

        public ConvertHlaRequester(IOptions<HlaMetadataDictionarySettings> settings)
        {
            hlaMetadataDictionarySettings = settings.Value;
        }

        public async Task<IEnumerable<string>> ConvertHla(ConvertHlaRequest request)
        {
            if (request?.HlaName is null)
            {
                throw new ArgumentException("ConvertHla request is missing required data.");
            }

            return await ExecuteHlaConversionRequest(request);
        }

        private async Task<IEnumerable<string>> ExecuteHlaConversionRequest(ConvertHlaRequest request)
        {
            var retryPolicy = Policy.Handle<Exception>().RetryAsync(10);

            var requestResponse = await retryPolicy.ExecuteAndCaptureAsync(async () => await SendHlaConversionRequest(request));

            return requestResponse.Outcome == OutcomeType.Successful
                ? requestResponse.Result
                : new[] { $"{FailedRequestPrefix} {request}" };
        }

        private async Task<IReadOnlyCollection<string>> SendHlaConversionRequest(ConvertHlaRequest request)
        {
            try
            {
                var response = await HttpRequestClient.PostAsync(
                    hlaMetadataDictionarySettings.ConvertHlaRequestUrl, new StringContent(JsonConvert.SerializeObject(request)));
                response.EnsureSuccessStatusCode();
                var hlaConversionResult = JsonConvert.DeserializeObject<List<string>>(await response.Content.ReadAsStringAsync());

                System.Diagnostics.Debug.WriteLine($"Result received: {request}");

                return hlaConversionResult;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{FailedRequestPrefix} {request}. Details: {ex.Message}. Re-attempting until success or re-attempt count reached.");
                throw;
            }
        }
    }
}