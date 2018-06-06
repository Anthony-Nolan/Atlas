using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Services
{
    public interface ISearchService
    {
        Task<IEnumerable<PotentialMatch>> Search(SearchRequest searchRequest);
    }
}