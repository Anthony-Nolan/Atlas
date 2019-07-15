using System.Threading.Tasks;
using Nova.SearchAlgorithm.Data.Persistent.Models;

namespace Nova.SearchAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshService
    {
        /// <summary>
        /// Runs a full donor import, followed by running an hla refresh on the newly imported donors.
        /// </summary>
        Task RefreshData(TransientDatabase databaseToRefresh, string wmdaDatabaseVersion);
    }
    
    public class DataRefreshService : IDataRefreshService
    {
        public async Task RefreshData(TransientDatabase databaseToRefresh, string wmdaDatabaseVersion)
        {
            throw new System.NotImplementedException();
        }
    }
}