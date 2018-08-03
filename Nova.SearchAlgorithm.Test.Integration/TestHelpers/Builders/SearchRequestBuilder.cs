using System;
using System.Collections.Generic;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
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
                MatchCriteria = new MismatchCriteria
                {
                    LocusMismatchA = new LocusMismatchCriteria(),
                    LocusMismatchB = new LocusMismatchCriteria(),
                    LocusMismatchDrb1 = new LocusMismatchCriteria()
                },
                SearchHlaData = new SearchHlaData
                {
                    LocusSearchHlaA = new LocusSearchHla(),
                    LocusSearchHlaB = new LocusSearchHla(),
                    LocusSearchHlaC = new LocusSearchHla(),
                    LocusSearchHlaDqb1 = new LocusSearchHla(),
                    LocusSearchHlaDrb1 = new LocusSearchHla(),
                }
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
                    searchRequest.MatchCriteria.LocusMismatchDqb1 = locusMatchCriteria;
                    break;
                case Locus.Drb1:
                    searchRequest.MatchCriteria.LocusMismatchDrb1 = locusMatchCriteria;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }

            return this;
        }
        
        public SearchRequestBuilder WithLocusMismatchCount(Locus locus, int locusMismatchCount)
        {
            switch (locus)
            {
                case Locus.A:
                    searchRequest.MatchCriteria.LocusMismatchA.MismatchCount = locusMismatchCount;
                    break;
                case Locus.B:
                    searchRequest.MatchCriteria.LocusMismatchB.MismatchCount = locusMismatchCount;
                    break;
                case Locus.C:
                    if (searchRequest.MatchCriteria.LocusMismatchC == null)
                    {
                        searchRequest.MatchCriteria.LocusMismatchC = new LocusMismatchCriteria();
                    }
                    searchRequest.MatchCriteria.LocusMismatchC.MismatchCount = locusMismatchCount;
                    break;
                case Locus.Dpb1:
                    throw new NotImplementedException();
                case Locus.Dqb1:
                    if (searchRequest.MatchCriteria.LocusMismatchDqb1 == null)
                    {
                        searchRequest.MatchCriteria.LocusMismatchDqb1 = new LocusMismatchCriteria();
                    }
                    searchRequest.MatchCriteria.LocusMismatchDqb1.MismatchCount = locusMismatchCount;
                    break;
                case Locus.Drb1:
                    searchRequest.MatchCriteria.LocusMismatchDrb1.MismatchCount = locusMismatchCount;
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
                        searchRequest.SearchHlaData.LocusSearchHlaA.SearchHla1 = hlaString;
                    }

                    if (positions == TypePositions.Two || positions == TypePositions.Both)
                    {
                        searchRequest.SearchHlaData.LocusSearchHlaA.SearchHla2 = hlaString;

                    }

                    break;
                case Locus.B:
                    if (positions == TypePositions.One || positions == TypePositions.Both)
                    {
                        searchRequest.SearchHlaData.LocusSearchHlaB.SearchHla1 = hlaString;
                    }

                    if (positions == TypePositions.Two || positions == TypePositions.Both)
                    {
                        searchRequest.SearchHlaData.LocusSearchHlaB.SearchHla2 = hlaString;
                    }

                    break;
                case Locus.C:
                    if (positions == TypePositions.One || positions == TypePositions.Both)
                    {
                        searchRequest.SearchHlaData.LocusSearchHlaC.SearchHla1 = hlaString;
                    }

                    if (positions == TypePositions.Two || positions == TypePositions.Both)
                    {
                        searchRequest.SearchHlaData.LocusSearchHlaC.SearchHla2 = hlaString;
                    }

                    break;
                case Locus.Dpb1:
                    throw new NotImplementedException();
                case Locus.Dqb1:
                    if (positions == TypePositions.One || positions == TypePositions.Both)
                    {
                        searchRequest.SearchHlaData.LocusSearchHlaDqb1.SearchHla1 = hlaString;
                    }

                    if (positions == TypePositions.Two || positions == TypePositions.Both)
                    {
                        searchRequest.SearchHlaData.LocusSearchHlaDqb1.SearchHla2 = hlaString;
                    }

                    break;
                case Locus.Drb1:
                    if (positions == TypePositions.One || positions == TypePositions.Both)
                    {
                        searchRequest.SearchHlaData.LocusSearchHlaDrb1.SearchHla1 = hlaString;
                    }

                    if (positions == TypePositions.Two || positions == TypePositions.Both)
                    {
                        searchRequest.SearchHlaData.LocusSearchHlaDrb1.SearchHla2 = hlaString;
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