using System;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.Utils.Http;
using AzureFunctions.Extensions.Swashbuckle;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using NJsonSchema;
using System.Net.Http;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Newtonsoft.Json;
using System.IO;
using Atlas.Functions.PublicApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Threading.Tasks;

namespace Atlas.Functions.PublicApi.Functions
{
    public class ApiDocumentationFunctions
    {
        private readonly ISwashBuckleClient swashbuckleClient;

        public ApiDocumentationFunctions(ISwashBuckleClient swashbuckleClient)
        {
            this.swashbuckleClient = swashbuckleClient;
        }

        [SwaggerIgnore]
        [Function(nameof(Swagger))]
        public Task<HttpResponseData> Swagger(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/json")]
            HttpRequestData req)
        {
            return swashbuckleClient.CreateSwaggerJsonDocumentResponse(req);
        }

        [SwaggerIgnore]
        [Function(nameof(SwaggerUi))]
        public Task<HttpResponseData> SwaggerUi(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/ui")]
            HttpRequestData req)
        {
            return swashbuckleClient.CreateSwaggerUIResponse(req, "swagger/json");
        }

        [Function(nameof(GenerateJsonSchemaForResultSet))]
        public string GenerateJsonSchemaForResultSet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = RouteConstants.SchemaRoutePrefix + "/resultSet")]
            [RequestBodyType(typeof(ResultSetSchemaGenerationRequest), nameof(ResultSetSchemaGenerationRequest))]
            HttpRequest request)
        {
            var requestBody = JsonConvert.DeserializeObject<ResultSetSchemaGenerationRequest>(new StreamReader(request.Body).ReadToEnd());

            if (!Enum.TryParse($"{requestBody.ResultSet}", out ResultSetOptions resultSetOption))
            {
                throw new BadHttpRequestException($"{requestBody} is not a member of {nameof(ResultSetOptions)}");
            }

            return resultSetOption switch
            {
                ResultSetOptions.OriginalSearch => GenerateJsonSchema<OriginalSearchResultSet>(),
                ResultSetOptions.RepeatSearch => GenerateJsonSchema<RepeatSearchResultSet>(),
                ResultSetOptions.OriginalMatchingAlgorithm => GenerateJsonSchema<OriginalMatchingAlgorithmResultSet>(),
                ResultSetOptions.RepeatMatchingAlgorithm => GenerateJsonSchema<RepeatMatchingAlgorithmResultSet>(),
                _ => throw new ArgumentOutOfRangeException(nameof(resultSetOption), resultSetOption, null)
            };
        }

        private static string GenerateJsonSchema<T>()
        {
            return JsonSchema.FromType<T>().ToJson();
        }
    }
}