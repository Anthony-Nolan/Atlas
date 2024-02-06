using System.Threading.Tasks;

namespace Atlas.Debug.Client.Clients
{
    /// <summary>
    /// Contains methods that are common to all Atlas function apps.
    /// </summary>
    public interface ICommonAtlasFunctions
    {
        /// <summary>
        /// Calls health check function.
        /// </summary>
        Task<string> HealthCheck();
    }
}
