using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Common.Models;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestHelpers
{
    public class SearchRequestBuilder
    {
        private readonly SearchRequest searchRequest;

        public SearchRequestBuilder()
        {
            searchRequest = new SearchRequest()
            {
                SearchType = DonorType.Adult,
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
                    LocusSearchHlaDrb1 = new LocusSearchHla(),
                },
                LociToExcludeFromAggregateScore = new List<Locus>()
            };
        }

        public SearchRequestBuilder WithTotalMismatchCount(int mismatchCount)
        {
            if (searchRequest.MatchCriteria == null)
            {
                searchRequest.MatchCriteria = new MismatchCriteria();
            }

            searchRequest.MatchCriteria.DonorMismatchCount = mismatchCount;
            return this;
        }

        public SearchRequestBuilder WithLocusMismatchCount(Locus locus, int locusMismatchCount)
        {
            if (searchRequest.MatchCriteria == null)
            {
                searchRequest.MatchCriteria = new MismatchCriteria();
            }

            switch (locus)
            {
                case Locus.A:
                    if (searchRequest.MatchCriteria.LocusMismatchA == null)
                    {
                        searchRequest.MatchCriteria.LocusMismatchA = new LocusMismatchCriteria();
                    }
                    searchRequest.MatchCriteria.LocusMismatchA.MismatchCount = locusMismatchCount;
                    break;
                case Locus.B:
                    if (searchRequest.MatchCriteria.LocusMismatchB == null)
                    {
                        searchRequest.MatchCriteria.LocusMismatchB = new LocusMismatchCriteria();
                    }
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
                    if (searchRequest.MatchCriteria.LocusMismatchDrb1 == null)
                    {
                        searchRequest.MatchCriteria.LocusMismatchDrb1 = new LocusMismatchCriteria();
                    }
                    searchRequest.MatchCriteria.LocusMismatchDrb1.MismatchCount = locusMismatchCount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }

            return this;
        }

        public SearchRequestBuilder WithMismatchCountAtLoci(IEnumerable<Locus> loci, int locusMismatchCount)
        {
            return loci.Aggregate(this, (current, locus) => current.WithLocusMismatchCount(locus, locusMismatchCount));
        }

        public SearchRequestBuilder WithLocusSearchHla(Locus locus, LocusPosition position, string hlaString)
        {
            if (searchRequest.SearchHlaData == null)
            {
                searchRequest.SearchHlaData = new SearchHlaData();
            }

            switch (locus)
            {
                case Locus.A:
                    if (searchRequest.SearchHlaData.LocusSearchHlaA == null)
                    {
                        searchRequest.SearchHlaData.LocusSearchHlaA = new LocusSearchHla();
                    }
                    switch (position)
                    {
                        case LocusPosition.Position1:
                            searchRequest.SearchHlaData.LocusSearchHlaA.SearchHla1 = hlaString;
                            break;
                        case LocusPosition.Position2:
                            searchRequest.SearchHlaData.LocusSearchHlaA.SearchHla2 = hlaString;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(position), position, null);
                    }

                    break;
                case Locus.B:
                    if (searchRequest.SearchHlaData.LocusSearchHlaB == null)
                    {
                        searchRequest.SearchHlaData.LocusSearchHlaB = new LocusSearchHla();
                    }
                    switch (position)
                    {
                        case LocusPosition.Position1:
                            searchRequest.SearchHlaData.LocusSearchHlaB.SearchHla1 = hlaString;
                            break;
                        case LocusPosition.Position2:
                            searchRequest.SearchHlaData.LocusSearchHlaB.SearchHla2 = hlaString;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(position), position, null);
                    }

                    break;
                case Locus.C:
                    if (searchRequest.SearchHlaData.LocusSearchHlaC == null)
                    {
                        searchRequest.SearchHlaData.LocusSearchHlaC = new LocusSearchHla();
                    }
                    switch (position)
                    {
                        case LocusPosition.Position1:
                            searchRequest.SearchHlaData.LocusSearchHlaC.SearchHla1 = hlaString;
                            break;
                        case LocusPosition.Position2:
                            searchRequest.SearchHlaData.LocusSearchHlaC.SearchHla2 = hlaString;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(position), position, null);
                    }

                    break;
                case Locus.Dpb1:
                    if (searchRequest.SearchHlaData.LocusSearchHlaDpb1 == null)
                    {
                        searchRequest.SearchHlaData.LocusSearchHlaDpb1 = new LocusSearchHla();
                    }
                    switch (position)
                    {
                        case LocusPosition.Position1:
                            searchRequest.SearchHlaData.LocusSearchHlaDpb1.SearchHla1 = hlaString;
                            break;
                        case LocusPosition.Position2:
                            searchRequest.SearchHlaData.LocusSearchHlaDpb1.SearchHla2 = hlaString;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(position), position, null);
                    }

                    break;
                case Locus.Dqb1:
                    if (searchRequest.SearchHlaData.LocusSearchHlaDqb1 == null)
                    {
                        searchRequest.SearchHlaData.LocusSearchHlaDqb1 = new LocusSearchHla();
                    }
                    switch (position)
                    {
                        case LocusPosition.Position1:
                            searchRequest.SearchHlaData.LocusSearchHlaDqb1.SearchHla1 = hlaString;
                            break;
                        case LocusPosition.Position2:
                            searchRequest.SearchHlaData.LocusSearchHlaDqb1.SearchHla2 = hlaString;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(position), position, null);
                    }

                    break;
                case Locus.Drb1:
                    if (searchRequest.SearchHlaData.LocusSearchHlaDrb1 == null)
                    {
                        searchRequest.SearchHlaData.LocusSearchHlaDrb1 = new LocusSearchHla();
                    }
                    switch (position)
                    {
                        case LocusPosition.Position1:
                            searchRequest.SearchHlaData.LocusSearchHlaDrb1.SearchHla1 = hlaString;
                            break;
                        case LocusPosition.Position2:
                            searchRequest.SearchHlaData.LocusSearchHlaDrb1.SearchHla2 = hlaString;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(position), position, null);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }

            return this;
        }

        public SearchRequestBuilder WithEmptyLocusSearchHlaAt(Locus locus)
        {
            switch (locus)
            {
                case Locus.A:
                    searchRequest.SearchHlaData.LocusSearchHlaA = null;
                    break;
                case Locus.B:
                    searchRequest.SearchHlaData.LocusSearchHlaB = null;
                    break;
                case Locus.C:
                    searchRequest.SearchHlaData.LocusSearchHlaC = null;
                    break;
                case Locus.Dpb1:
                    searchRequest.SearchHlaData.LocusSearchHlaDpb1 = null;
                    break;
                case Locus.Dqb1:
                    searchRequest.SearchHlaData.LocusSearchHlaDqb1 = null;
                    break;
                case Locus.Drb1:
                    searchRequest.SearchHlaData.LocusSearchHlaDrb1 = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }

            return this;
        }

        public SearchRequestBuilder WithSearchType(DonorType donorType)
        {
            searchRequest.SearchType = donorType;
            return this;
        }

        public SearchRequestBuilder WithSearchHla(Common.Models.PhenotypeInfo<string> searchHla)
        {
            return WithLocusSearchHla(Locus.A, LocusPosition.Position1, searchHla.A.Position1)
                .WithLocusSearchHla(Locus.A, LocusPosition.Position2, searchHla.A.Position2)
                .WithLocusSearchHla(Locus.B, LocusPosition.Position1, searchHla.B.Position1)
                .WithLocusSearchHla(Locus.B, LocusPosition.Position2, searchHla.B.Position2)
                .WithLocusSearchHla(Locus.C, LocusPosition.Position1, searchHla.C.Position1)
                .WithLocusSearchHla(Locus.C, LocusPosition.Position2, searchHla.C.Position2)
                .WithLocusSearchHla(Locus.Dpb1, LocusPosition.Position1, searchHla.Dpb1.Position1)
                .WithLocusSearchHla(Locus.Dpb1, LocusPosition.Position2, searchHla.Dpb1.Position2)
                .WithLocusSearchHla(Locus.Dqb1, LocusPosition.Position1, searchHla.Dqb1.Position1)
                .WithLocusSearchHla(Locus.Dqb1, LocusPosition.Position2, searchHla.Dqb1.Position2)
                .WithLocusSearchHla(Locus.Drb1, LocusPosition.Position1, searchHla.Drb1.Position1)
                .WithLocusSearchHla(Locus.Drb1, LocusPosition.Position2, searchHla.Drb1.Position2);
        }

        public SearchRequestBuilder WithSearchHla(Atlas.Common.GeneticData.PhenotypeInfo.PhenotypeInfo<string> searchHla)
        {
            return WithSearchHla(new Common.Models.PhenotypeInfo<string>(searchHla));
        }
        
        public SearchRequestBuilder WithLociExcludedFromScoringAggregates(IEnumerable<Locus> loci)
        {
            searchRequest.LociToExcludeFromAggregateScore = loci;
            return this;
        }

        public SearchRequest Build()
        {
            return searchRequest;
        }
    }
}