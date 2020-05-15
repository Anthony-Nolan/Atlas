using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.Common.GeneticData;
using Locus = Atlas.Common.GeneticData.Locus;

namespace Atlas.MatchingAlgorithm.Test.Builders
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
                LociToExcludeFromAggregateScore = new List<LocusType>()
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

        public SearchRequestBuilder WithMismatchCountAtLoci(IEnumerable<Locus> loci, int locusMismatchCount)
        {
            return loci.Aggregate(this, (current, locus) => current.WithLocusMismatchCount(locus, locusMismatchCount));
        }

        public SearchRequestBuilder WithLocusMatchHla(Locus locus, TypePosition position, string hlaString)
        {
            // API level validation will fail if individual hla are null, but not if the locus is omitted altogether. 
            // If tests need to be added which set individual values to null (i.e. to test that validation), another builder method should bve added
            if (hlaString == null)
            {
                return this;
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
                        case TypePosition.One:
                            searchRequest.SearchHlaData.LocusSearchHlaA.SearchHla1 = hlaString;
                            break;
                        case TypePosition.Two:
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
                        case TypePosition.One:
                            searchRequest.SearchHlaData.LocusSearchHlaB.SearchHla1 = hlaString;
                            break;
                        case TypePosition.Two:
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
                        case TypePosition.One:
                            searchRequest.SearchHlaData.LocusSearchHlaC.SearchHla1 = hlaString;
                            break;
                        case TypePosition.Two:
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
                        case TypePosition.One:
                            searchRequest.SearchHlaData.LocusSearchHlaDpb1.SearchHla1 = hlaString;
                            break;
                        case TypePosition.Two:
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
                        case TypePosition.One:
                            searchRequest.SearchHlaData.LocusSearchHlaDqb1.SearchHla1 = hlaString;
                            break;
                        case TypePosition.Two:
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
                        case TypePosition.One:
                            searchRequest.SearchHlaData.LocusSearchHlaDrb1.SearchHla1 = hlaString;
                            break;
                        case TypePosition.Two:
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

        public SearchRequestBuilder WithSearchType(DonorType donorType)
        {
            searchRequest.SearchType = donorType;
            return this;
        }

        public SearchRequestBuilder WithSearchHla(PhenotypeInfo<string> searchHla)
        {
            return WithLocusMatchHla(Locus.A, TypePosition.One, searchHla.A.Position1)
                .WithLocusMatchHla(Locus.A, TypePosition.Two, searchHla.A.Position2)
                .WithLocusMatchHla(Locus.B, TypePosition.One, searchHla.B.Position1)
                .WithLocusMatchHla(Locus.B, TypePosition.Two, searchHla.B.Position2)
                .WithLocusMatchHla(Locus.C, TypePosition.One, searchHla.C.Position1)
                .WithLocusMatchHla(Locus.C, TypePosition.Two, searchHla.C.Position2)
                .WithLocusMatchHla(Locus.Dpb1, TypePosition.One, searchHla.Dpb1.Position1)
                .WithLocusMatchHla(Locus.Dpb1, TypePosition.Two, searchHla.Dpb1.Position2)
                .WithLocusMatchHla(Locus.Dqb1, TypePosition.One, searchHla.Dqb1.Position1)
                .WithLocusMatchHla(Locus.Dqb1, TypePosition.Two, searchHla.Dqb1.Position2)
                .WithLocusMatchHla(Locus.Drb1, TypePosition.One, searchHla.Drb1.Position1)
                .WithLocusMatchHla(Locus.Drb1, TypePosition.Two, searchHla.Drb1.Position2);
        }

        public SearchRequest Build()
        {
            return searchRequest;
        }
    }
}