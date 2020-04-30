namespace Atlas.Utils.ServiceClient
{
    /// <summary>
    /// Extension to be used for function specific http clients.
    /// This enforces the use of a functions specific client, as some details e.g. authentication may be different
    /// </summary>
    public interface INovaFunctionsHttpClient : INovaHttpClient
    {
    }
}