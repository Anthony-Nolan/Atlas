using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Atlas.Utils.ServiceClient
{
    /// <summary>
    /// Declares functionality necessary of a Nova Http client.
    /// Different implementations of this class may be needed, depending on the dependencies necessary.
    /// For applications tied to an older version of Newtonsoft.Json (e.g. azure functions v1 at 9.0.1), a legacy client should be used, Atlas.Utils.Client.Legacy.
    /// For all other applications, Atlas.Utils.Client should suffice. 
    /// </summary>
    public interface INovaHttpClient
    {
        /// <summary>
        /// Builds an HTTP request object.
        /// </summary>
        /// <param name="method">The HTTP method to be used.</param>
        /// <param name="pathname"></param>
        /// <param name="parameters"></param>
        /// <param name="body"></param>
        /// <returns>The built HTTP message</returns>
        HttpRequestMessage GetRequest(HttpMethod method, string pathname, List<KeyValuePair<string, string>> parameters = null, object body = null);

        IEnumerable<KeyValuePair<string, string>> GenerateParametersForList(string key, IEnumerable<string> values);

        /// <summary>
        /// Makes an HTTP request, without expecting a response
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task MakeRequestAsync(HttpRequestMessage request);

        /// <summary>
        /// Makes an HTTP request, returning the response deserialized to an object of type T
        /// </summary>
        /// <param name="request"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<T> MakeRequestAsync<T>(HttpRequestMessage request);
    }
}