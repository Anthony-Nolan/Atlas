namespace Atlas.Utils.ServiceClient
{
    /// <summary>
    /// This class can be extended, and individual endpoints for the implementing service added.
    /// No HTTP client functionality is provided by this package directly - instead, an HttpClient should be injected
    /// This allows for either the legacy or non-legacy version to be injected. 
    /// </summary>
    public abstract class FunctionsClientBase
    {
        protected readonly INovaFunctionsHttpClient HttpClient;
        
        protected FunctionsClientBase(INovaFunctionsHttpClient novaHttpClient)
        {
            HttpClient = novaHttpClient;
        }
    }
}