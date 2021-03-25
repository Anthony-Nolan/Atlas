using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.Client.Models.Search.Results.ResultSet
{
    public abstract class ResultSet<TResult> where TResult : Result
    {
        public string SearchRequestId { get; set; }

        public abstract bool IsRepeatSearchSet { get; }

        public string HlaNomenclatureVersion { get; set; }
        public string BlobStorageContainerName { get; set; }
        public abstract string ResultsFileName { get; }

        public int TotalResults { get; set; }
        public IEnumerable<TResult> Results { get; set; }

        /// <summary>
        /// The HLA that the search was run against.
        /// </summary>
        public PhenotypeInfoTransfer<string> SearchedHla { get; set; }
    }
}