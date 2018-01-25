using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories.SearchRequests;

namespace Nova.SearchAlgorithm.Services
{
    public interface ISearchRequestService
    {
        int? CreateSearchRequest(SearchRequestCreationModel searchRequest);
    }
    public class SearchRequestService : ISearchRequestService
    {
        private readonly ISearchRequestRepository repository;

        public SearchRequestService(ISearchRequestRepository searchRequestRepository)
        {
            repository = searchRequestRepository;
        }

        public int? CreateSearchRequest(SearchRequestCreationModel searchRequest)
        {
            //todo: NOVA-761 - decide what kind of object to return
            var searchRequestId = repository.CreateSearchRequest(searchRequest);
            return searchRequestId;
        }
    }
}