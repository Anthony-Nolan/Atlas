using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshOrchestrator
    {
        Task RefreshDataIfNecessary();
    }
    
    public class DataRefreshOrchestrator : IDataRefreshOrchestrator
    {
        public async Task RefreshDataIfNecessary()
        {
            throw new System.NotImplementedException();
        }
    }
}