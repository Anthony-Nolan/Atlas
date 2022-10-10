using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Atlas.Functions.PublicApi.Test.Manual.Helpers
{
    internal static class RequestExtension
    {
        public static async Task<T> DeserialiseRequestBody<T>(this HttpRequest request)
        {
            return JsonConvert.DeserializeObject<T>(
                await new StreamReader(request.Body).ReadToEndAsync());
        }
    }
}
