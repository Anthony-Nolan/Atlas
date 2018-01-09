using System.Configuration;
using System.Net;
using Castle.Core.Internal;
using Nova.SearchAlgorithm.Client.Models;
using Nova.Utils.Http.Exceptions;
using Nova.Utils.WebApi.Filters;
using RestSharp;

namespace Nova.SearchAlgorithm.Client
{
    public class TemplateServiceClient
    {
        private readonly string baseUrl;
        private readonly string apiKey;

        public TemplateServiceClient(string baseUrl, string apiKey = null)
        {
            this.baseUrl = baseUrl;
            this.apiKey = apiKey;
        }

        public TemplateResponseModel Get(string id)
        {
            var client = GetClient();
            var request = new RestRequest("api/templates/{id}");
            request.AddParameter("id", id);
            request.RootElement = "Template";
            var response = client.Execute<TemplateResponseModel>(request);

            AssertResponseOk(response);
            return response.Data;
        }

        private RestClient GetClient()
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