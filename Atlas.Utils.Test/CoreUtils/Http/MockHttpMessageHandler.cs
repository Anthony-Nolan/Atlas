using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Utils.Test.CoreUtils.Http
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage Request { get; private set; }
        public HttpResponseMessage Response { private get; set; }
        public string RequestContent { get; private set; }
        public Exception ErrorToThrow { private get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (ErrorToThrow != null)
            {
                throw ErrorToThrow;
            }
            Request = request;
            if (request.Content != null)
            {
                RequestContent = await request.Content.ReadAsStringAsync();
            }
            return Response;
        }
    }
}
