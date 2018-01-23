using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Services
{
    public interface ISearchRequestService
    {
        Task<int?> CreateSearchRequest(SearchRequestCreationModel searchRequest);
    }
    public class SearchRequestService : ISearchRequestService
    {
        public async Task<int?> CreateSearchRequest(SearchRequestCreationModel searchRequest)
        {
            //todo: NOVA-749: add code to return a new search request id, once the repository has been setup 
            return await Task.FromResult((int?)0);
        }
    }
}