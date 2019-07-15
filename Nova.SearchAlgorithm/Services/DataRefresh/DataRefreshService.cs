using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshService
    {
        /// <summary>
        /// Runs a full donor import, followed by running an hla refresh on the newly imported donors.
        /// </summary>
        /// <returns></returns>
        Task RefreshData();
    }
    
    public class DataRefreshService : IDataRefreshService
    {
        public async Task RefreshData()
        {
            throw new System.NotImplementedException();
        }
    }
}