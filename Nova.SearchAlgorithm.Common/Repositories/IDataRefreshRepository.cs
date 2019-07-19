using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Common.Repositories
{
    /// <summary>
    /// Provides methods indicating which donors have already been imported / processed 
    /// </summary>
    public interface IDataRefreshRepository
    {
        Task<int> HighestDonorId();
        Task<IBatchQueryAsync<DonorResult>> DonorsAddedSinceLastHlaUpdate(int batchSize);
        Task<int> GetDonorCount();
    }
}