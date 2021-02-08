// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.Client.Models.Search.Requests
{
    public class SearchInitiationResponse
    {
        public string SearchIdentifier { get; set; }
        
        /// <summary>
        /// When running a search for the first time, this will be null.
        /// When running a repeat search, allows distinguishing of each incremental result set. 
        /// </summary>
        public string RepeatSearchIdentifier { get; set; }
    }
}