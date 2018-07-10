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
        
        public SearchRequestBuilder WithLocusMatchCount(Locus locus, int locusMatchCount)
        {
            switch (locus)
            {
                case Locus.A:
                    searchRequest.MatchCriteria.LocusMismatchA.MismatchCount = locusMatchCount;
                    break;
                case Locus.B:
                    searchRequest.MatchCriteria.LocusMismatchB.MismatchCount = locusMatchCount;
                    break;
                case Locus.C:
                    searchRequest.MatchCriteria.LocusMismatchC.MismatchCount = locusMatchCount;
                    break;
                case Locus.Dpb1:
                    throw new NotImplementedException();
                case Locus.Dqb1:
                    searchRequest.MatchCriteria.LocusMismatchDQB1.MismatchCount = locusMatchCount;
                    break;
                case Locus.Drb1:
                    searchRequest.MatchCriteria.LocusMismatchDRB1.MismatchCount = locusMatchCount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }

            return this;
        }
        
        public SearchRequestBuilder WithLocusMatchHla(Locus locus, TypePositions positions, string hlaString)
        {
            switch (locus)
            {
                case Locus.A:
                    if (positions == TypePositions.One || positions == TypePositions.Both)
                    {
                        searchRequest.MatchCriteria.LocusMismatchA.SearchHla1 = hlaString;
                    }

                    if (positions == TypePositions.Two || positions == TypePositions.Both)
                    {
                        searchRequest.MatchCriteria.LocusMismatchA.SearchHla2 = hlaString;

                    }

                    break;
                case Locus.B:
                    if (positions == TypePositions.One || positions == TypePositions.Both)
                    {
                        searchRequest.MatchCriteria.LocusMismatchB.SearchHla1 = hlaString;
                    }

                    if (positions == TypePositions.Two || positions == TypePositions.Both)
                    {
                        searchRequest.MatchCriteria.LocusMismatchB.SearchHla2 = hlaString;
                    }

                    break;
                case Locus.C:
                    if (positions == TypePositions.One || positions == TypePositions.Both)
                    {
                        searchRequest.MatchCriteria.LocusMismatchC.SearchHla1 = hlaString;
                    }

                    if (positions == TypePositions.Two || positions == TypePositions.Both)
                    {
                        searchRequest.MatchCriteria.LocusMismatchC.SearchHla2 = hlaString;
                    }

                    break;
                case Locus.Dpb1:
                    throw new NotImplementedException();
                case Locus.Dqb1:
                    if (positions == TypePositions.One || positions == TypePositions.Both)
                    {
                        searchRequest.MatchCriteria.LocusMismatchDQB1.SearchHla1 = hlaString;
                    }

                    if (positions == TypePositions.Two || positions == TypePositions.Both)
                    {
                        searchRequest.MatchCriteria.LocusMismatchDQB1.SearchHla2 = hlaString;
                    }

                    break;
                case Locus.Drb1:
                    if (positions == TypePositions.One || positions == TypePositions.Both)
                    {
                        searchRequest.MatchCriteria.LocusMismatchDRB1.SearchHla1 = hlaString;
                    }

                    if (positions == TypePositions.Two || positions == TypePositions.Both)
                    {
                        searchRequest.MatchCriteria.LocusMismatchDRB1.SearchHla2 = hlaString;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }

            return this;
        }

        public SearchRequest Build()
        {
            return searchRequest;
        }
    }
}