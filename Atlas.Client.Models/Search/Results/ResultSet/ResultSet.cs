﻿using System;
using System.Collections.Generic;
using Atlas.Client.Models.Common.Requests;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Newtonsoft.Json;

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

        /// <summary>
        /// The match count denominator based on number of loci submitted in <see cref="MismatchCriteria.LocusMismatchCriteria"/>.
        /// Will be `null` if <see cref="SearchRequest"/> (or locus mismatch criteria within the request) are not provided.
        /// </summary>
        public int? MatchCriteriaDenominator
        {
            get
            {
                var matchLociCount = SearchRequest?.MatchCriteria?.LocusMismatchCriteria
                    .ToLociInfo()
                    .Reduce((_, value, accumulator) => value is null ? accumulator : ++accumulator, 0);

                return matchLociCount * 2;
            }
        }

        /// <summary>
        /// The match count denominator based on <see cref="ScoringCriteria.LociToScore"/> minus <see cref="ScoringCriteria.LociToExcludeFromAggregateScore"/>.
        /// If scoring has not been requested for any loci, then <see cref="ScoringCriteriaDenominator"/> will be `null`.
        /// Will also be `null` if <see cref="SearchRequest"/> is not provided.
        /// </summary>
        public int? ScoringCriteriaDenominator
        {
            get
            {
                var scoreCount = SearchRequest?.ScoringCriteria?.LociToScore?.Count;
                var excludeFromAggregateScoreCount = SearchRequest?.ScoringCriteria?.LociToExcludeFromAggregateScore?.Count;
                var includeInAggregateScoreCount = scoreCount is null or 0 ? null : scoreCount - (excludeFromAggregateScoreCount ?? 0);

                return includeInAggregateScoreCount * 2;
            }
        }

        public IEnumerable<TResult> Results { get; set; }

        /// <summary>
        /// This method is used to control serialization of Results property
        /// When this method returns true - <see cref="Results" /> will be serialized, when it's false - <see cref="Results" /> won't be serialized
        /// https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm
        /// </summary>
        public bool ShouldSerializeResults() => !BatchedResult;


        /// <summary>
        /// This property is used to be able to control in runtime if <see cref="Results" /> should be serialized or not
        /// </summary>
        [JsonIgnore]
        public bool BatchedResult { get; set; } = false;

        /// <summary>
        /// The <see cref="SearchRequest"/> that this result set is for. Not strictly necessary for consuming results, but can be very useful for
        /// debugging / support purposes, removing the need to cross reference result sets to request details.  
        /// </summary>
        public SearchRequest SearchRequest { get; set; }

        /// <summary>
        /// DateTime that the matching request was picked up for processing (equivalent to dequeued time for queued requests)
        /// </summary>
        public DateTimeOffset MatchingStartTime { get; set; }
    }
}