using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Models;

namespace Atlas.MatchingAlgorithm.Clients.Http.DonorService
{
    public interface IDonorServiceClient
    {
        /// <summary>
        /// Returns a page of donors information which is required for the new search algorithm.
        /// These donors Info only contain the information required to do a search with the new algorithm and not for display in the frontend.
        /// Useful for any client wishing to process all donors information one page at a time.
        /// </summary>
        /// <param name="resultsPerPage">The number of donors required per page</param>
        /// <param name="lastId">The last ID of the previous page. This pagination system is to make sure
        /// that any client paging through donors won't miss out if donors are inserted or deleted in-between page requests.
        /// If null or omitted, the first page of results will be returned.</param>
        /// <returns>A page of donors Info for search algorithm</returns>
        Task<SearchableDonorInformationPage> GetDonorsInfoForSearchAlgorithm(int resultsPerPage, int lastId);
    }
}