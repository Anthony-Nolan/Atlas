namespace Atlas.Utils.ServiceClient
{
    /// <summary>
    /// This class can be extended, and individual endpoints for the implementing service added.
    /// No HTTP client functionality is provided by this package directly - instead, an HttpClient should be injected
    /// This allows for either the legacy or non-legacy version to be injected. 
    /// </summary>
    public abstract class ServiceClientBase
    {
        protected readonly INovaHttpClient HttpClient;
        
        protected ServiceClientBase(INovaHttpClient novaHttpClient)
        {
            HttpClient = novaHttpClient;
        }
    }
}