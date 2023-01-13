using System.Collections.Generic;
using Atlas.Client.Models.Search.Requests;

namespace Atlas.Client.Models.Search.Results.ResultSet
{
    public abstract class ResultSet<TResult> where TResult : Result
    {
        public string SearchRequestId { get; set; }

        public abstract bool IsRepeatSearchSet { get; }

        public string MatchingAlgorithmHlaNomenclatureVersion { get; set; }
        public string BlobStorageContainerName { get; set; }
        public abstract string ResultsFileName { get; }

        public int TotalResults { get; set; }
        public IEnumerable<TResult> Results { get; set; }

        /// <summary>
        /// The <see cref="SearchRequest"/> that this result set is for. Not strictly necessary for consuming results, but can be very useful for
        /// debugging / support purposes, removing the need to cross reference result sets to request details.  
        /// </summary>
        public SearchRequest SearchRequest { get; set; }
    }
}