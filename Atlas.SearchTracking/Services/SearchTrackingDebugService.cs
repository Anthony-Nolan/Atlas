using Atlas.Debug.Client.Models.SearchTracking;
using Atlas.SearchTracking.Data.Repositories;
using AutoMapper;

namespace Atlas.SearchTracking.Services
{
    public interface ISearchTrackingDebugService
    {
        Task<SearchTrackingSearchRequest> GetSearchRequestByIdentifier(Guid searchIdentifier);
    }

    public class SearchTrackingDebugService : ISearchTrackingDebugService
    {
        private readonly ISearchRequestRepository searchRequestRepository;
        private readonly IMapper mapper;

        public SearchTrackingDebugService(ISearchRequestRepository searchRequestRepository, IMapper mapper)
        {
            this.searchRequestRepository = searchRequestRepository;
            this.mapper = mapper;
        }

        public async Task<SearchTrackingSearchRequest> GetSearchRequestByIdentifier(Guid searchIdentifier)
        {
            var searchRequest = await searchRequestRepository.GetSearchRequestWithLinkedEntitiesByIdentifier(searchIdentifier);
            return mapper.Map<SearchTrackingSearchRequest>(searchRequest);
        }
    }
}
