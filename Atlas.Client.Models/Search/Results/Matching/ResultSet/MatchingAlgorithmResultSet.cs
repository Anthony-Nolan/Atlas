﻿using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.Client.Models.Search.Results.Matching.ResultSet
{
    public abstract class MatchingAlgorithmResultSet
    {
        public string SearchRequestId { get; set; }
        public abstract bool IsRepeatSearchSet { get; }
        public int ResultCount { get; set; }
        public IEnumerable<MatchingAlgorithmResult> MatchingAlgorithmResults { get; set; }

        public string HlaNomenclatureVersion { get; set; }
        public string BlobStorageContainerName { get; set; }
        public abstract string ResultsFileName { get; }
        
        /// <summary>
        /// The HLA that the search was run against.
        /// </summary>
        public PhenotypeInfoTransfer<string> SearchedHla { get; set; }
    }
}