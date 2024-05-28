using System.Net.Http;
using System.Threading.Tasks;
using AzureFunctions.Extensions.Swashbuckle;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class SwaggerFunctions
    {
        private readonly ISwashBuckleClient swashBuckleClient;
        public SwaggerFunctions(ISwashBuckleClient swashbuckleClient)
        {
            this.swashBuckleClient = swashbuckleClient;
        }

        [SwaggerIgnore]
        [Function(nameof(Swagger))]
        public Task<HttpResponseData> Swagger(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/json")]
            HttpRequestData req
            )
        {
            return swashBuckleClient.CreateSwaggerJsonDocumentResponse(req);
        }

        [SwaggerIgnore]
        [Function(nameof(SwaggerUi))]
        public Task<HttpResponseData> SwaggerUi(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/ui")]
            HttpRequestData req
            )
        {
            return swashBuckleClient.CreateSwaggerUIResponse(req, "swagger/json");
        }
    }
}