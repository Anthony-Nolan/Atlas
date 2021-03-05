using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders
{
    /// <summary>
    /// Build search requests from the submitted HLA typings.
    /// The typings at specified loci positions can be easily altered to induce mismatched scenarios.
    /// </summary>
    public class SearchRequestFromHlasBuilder
    {
        private readonly PhenotypeInfo<string> nonMatchingHlas;
        private SearchRequestBuilder searchRequestBuilder;

        /// <param name="searchHlas">A selection of valid HLA data.</param>
        /// <param name="nonMatchingHlas">A selection of valid hla strings that do not match the search HLA.</param>
        public SearchRequestFromHlasBuilder(PhenotypeInfo<string> searchHlas, PhenotypeInfo<string> nonMatchingHlas = null)
        {
            searchRequestBuilder = new SearchRequestBuilder()
                .WithSearchType(DonorType.Adult)
                .WithLociToScore(new List<Locus>())
                .WithLociExcludedFromScoringAggregates(new List<Locus>())
                .WithSearchHla(searchHlas);
            this.nonMatchingHlas = nonMatchingHlas;
        }

        public SearchRequestFromHlasBuilder TenOutOfTen()
        {
            searchRequestBuilder = searchRequestBuilder
                .WithTotalMismatchCount(0)
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.C, 0)
                .WithLocusMismatchCount(Locus.Dqb1, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0);
            return this;
        }

        public SearchRequestFromHlasBuilder SixOutOfSix()
        {
            searchRequestBuilder = searchRequestBuilder
                .WithTotalMismatchCount(0)
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0);
            return this;
        }

        public SearchRequestFromHlasBuilder FiveOutOfSix()
        {
            searchRequestBuilder = searchRequestBuilder.WithTotalMismatchCount(1);
            return this;
        }

        public SearchRequestFromHlasBuilder FourOutOfSix()
        {
            searchRequestBuilder = searchRequestBuilder.WithTotalMismatchCount(2);
            return this;
        }

        public SearchRequestFromHlasBuilder WithSingleMismatchRequestedAt(Locus locus)
        {
            searchRequestBuilder = searchRequestBuilder
                .WithLocusMismatchCount(Locus.A, locus == Locus.A ? 1 : 0)
                .WithLocusMismatchCount(Locus.B, locus == Locus.B ? 1 : 0)
                .WithLocusMismatchCount(Locus.Drb1, locus == Locus.Drb1 ? 1 : 0);
            return this;
        }

        public SearchRequestFromHlasBuilder WithDoubleMismatchRequestedAt(Locus locus)
        {
            searchRequestBuilder = searchRequestBuilder
                .WithLocusMismatchCount(Locus.A, locus == Locus.A ? 2 : 0)
                .WithLocusMismatchCount(Locus.B, locus == Locus.B ? 2 : 0)
                .WithLocusMismatchCount(Locus.Drb1, locus == Locus.Drb1 ? 2 : 0);
            return this;
        }

        public SearchRequestFromHlasBuilder WithPositionOneOfSearchHlaMismatchedAt(Locus locus)
        {
            if (nonMatchingHlas == null)
            {
                throw new InvalidOperationException("Non-matching HLA data has not been provided.");
            }

            searchRequestBuilder = searchRequestBuilder
                .WithLocusSearchHla(locus, LocusPosition.One, nonMatchingHlas.GetLocus(locus).Position1);
            return this;
        }

        public SearchRequestFromHlasBuilder WithAllLociScored()
        {
            searchRequestBuilder.WithLociToScore(EnumStringValues.EnumExtensions.EnumerateValues<Locus>().ToList());
            return this;
        }

        public SearchRequestFromHlasBuilder WithLociToScore(IReadOnlyCollection<Locus> loci)
        {
            searchRequestBuilder.WithLociToScore(loci);
            return this;
        }

        public SearchRequestFromHlasBuilder WithDpb1ExcludedFromScoringAggregation()
        {
            searchRequestBuilder.WithLociExcludedFromScoringAggregates(new List<Locus> {Locus.Dpb1});
            return this;
        }

        public SearchRequestFromHlasBuilder WithNullLocusSearchHlasAt(Locus locus)
        {
            searchRequestBuilder.WithNullLocusSearchHla(locus);
            return this;
        }

        public SearchRequest Build()
        {
            return searchRequestBuilder.Build();
        }
    }
}