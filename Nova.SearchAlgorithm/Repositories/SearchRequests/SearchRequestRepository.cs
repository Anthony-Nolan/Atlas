using AutoMapper;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories.SearchRequests.AzureStorage;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Repositories.SearchRequests
{
    public interface ISearchRequestRepository
    {
        int CreateSearchRequest(SearchRequest searchRequest);
    }

    public class SearchRequestRepository : ISearchRequestRepository
    {
        private const string TableReference = "SearchRequests";
        private readonly CloudTable selectedSearchRequestTable;
        private readonly IMapper mapper;

        public SearchRequestRepository(IMapper mapper, ICloudTableFactory cloudTableFactory)
        {
            selectedSearchRequestTable = cloudTableFactory.GetTable(TableReference);
            this.mapper = mapper;
        }

        public int CreateSearchRequest(SearchRequest searchRequest)
        {
            var operation = TableOperation.Insert(searchRequest.ToTableEntity(mapper));
            selectedSearchRequestTable.Execute(operation);

            //todo: NOVA-761 - decide what kind of object to return
            return 0;
        }
    }
}
