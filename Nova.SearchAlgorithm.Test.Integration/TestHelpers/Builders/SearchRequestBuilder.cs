using System;
using System.Collections.Generic;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders
{
    public class SearchRequestBuilder
    {
        private readonly SearchRequest searchRequest;
        
        public SearchRequestBuilder()
        {
            searchRequest = new SearchRequest()
            {
                SearchType = DonorType.Adult,
                RegistriesToSearch = new List<RegistryCode> { RegistryCode.AN },
                MatchCriteria = new MismatchCriteria()
            };
        }
        
        public SearchRequestBuilder WithTotalMismatchCount(int mismatchCount)
        {
            searchRequest.MatchCriteria.DonorMismatchCount = mismatchCount;
            return this;
        }

        public SearchRequestBuilder WithLocusMatchCriteria(Locus locus, LocusMismatchCriteria locusMatchCriteria)
        {
            switch (locus)
            {
                case Locus.A:
                    searchRequest.MatchCriteria.LocusMismatchA = locusMatchCriteria;
                    break;
                case Locus.B:
                    searchRequest.MatchCriteria.LocusMismatchB = locusMatchCriteria;
                    break;
                case Locus.C:
                    searchRequest.MatchCriteria.LocusMismatchC = locusMatchCriteria;
                    break;
                case Locus.Dpb1:
                    throw new NotImplementedException();
                case Locus.Dqb1:
                    searchRequest.MatchCriteria.LocusMismatchDQB1 = locusMatchCriteria;
                    break;
                case Locus.Drb1:
                    searchRequest.MatchCriteria.LocusMismatchDRB1 = locusMatchCriteria;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }

            return this;
        }
        
        // Populates all null required match criteria (A, B, DRB) with given value
        public SearchRequestBuilder WithDefaultLocusMatchCriteria(LocusMismatchCriteria locusMatchCriteria)
        {
            searchRequest.MatchCriteria.LocusMismatchA = searchRequest.MatchCriteria.LocusMismatchA ?? locusMatchCriteria;
            searchRequest.MatchCriteria.LocusMismatchB = searchRequest.MatchCriteria.LocusMismatchB ?? locusMatchCriteria;
            searchRequest.MatchCriteria.LocusMismatchDRB1 = searchRequest.MatchCriteria.LocusMismatchDRB1 ?? locusMatchCriteria;
            return this;
        }

        public SearchRequest Build()
        {
            return searchRequest;
        }
    }
}