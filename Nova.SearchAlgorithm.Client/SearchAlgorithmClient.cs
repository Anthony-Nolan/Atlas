using Nova.SearchAlgorithm.Client.Models;
using Nova.Utils.Http.Exceptions;
using Nova.Utils.WebApi.Filters;
using RestSharp;
using System.Configuration;
using System.Net;

namespace Nova.SearchAlgorithm.Client
{
    public interface ISearchAlgorithmClient
    {
        IRestResponse CreateSearchRequest(SearchRequest searchRequest);
    }

    public class SearchAlgorithmClient: ISearchAlgorithmClient
    {
        private readonly string baseUrl;
        private readonly string apiKey;

        public SearchAlgorithmClient(string baseUrl, string apiKey = null)
        {
            this.baseUrl = baseUrl;
            this.apiKey = apiKey;
        }

        public IRestResponse CreateSearchRequest(SearchRequest searchRequest)
        {
            var client = GetSearchAlgorithmClient();
            var request = new RestRequest("create-search-request", Method.POST) { RequestFormat = DataFormat.Json };
            request.AddBody(searchRequest);
            var response = client.Execute(request);

            AssertResponseOk(response);
            return response;
        }

        private RestClient GetSearchAlgorithmClient()
        {
            var client = new RestClient(baseUrl);
            client.AddDefaultHeader(ApiKeyRequiredAttribute.HEADER_KEY, GetApiKey());
            return client;
        }

        private string GetApiKey()
        {
            if (apiKey != null)
                return apiKey;

            // TODO NOVA-367 - This is really hacky.
            var configApiKey = ConfigurationManager.AppSettings["ReportServiceApiKey"];
            if (configApiKey != null)
                return configApiKey;

            throw new ConfigurationErrorsException("No API key found.");
        }

        private static void AssertResponseOk(IRestResponse response)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return;
                case HttpStatusCode.NotFound:
                    throw new NovaNotFoundException(response.ErrorMessage);
                default:
                    throw new NovaHttpException(response.StatusCode, response.ErrorMessage, response.ErrorException);
            }
        }
    }
}