using System;
using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Data.Models;
using Locus = Atlas.Common.GeneticData.Locus;

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
                .WithLociExcludedFromScoringAggregates(new List<Locus>())
                .WithLocusSearchHla(Locus.A, LocusPosition.One, searchHlas.A.Position1)
                .WithLocusSearchHla(Locus.A, LocusPosition.Two, searchHlas.A.Position2)
                .WithLocusSearchHla(Locus.B, LocusPosition.One, searchHlas.B.Position1)
                .WithLocusSearchHla(Locus.B, LocusPosition.Two, searchHlas.B.Position2)
                .WithLocusSearchHla(Locus.C, LocusPosition.One, searchHlas.C.Position1)
                .WithLocusSearchHla(Locus.C, LocusPosition.Two, searchHlas.C.Position2)
                .WithLocusSearchHla(Locus.Dpb1, LocusPosition.One, searchHlas.Dpb1.Position1)
                .WithLocusSearchHla(Locus.Dpb1, LocusPosition.Two, searchHlas.Dpb1.Position2)
                .WithLocusSearchHla(Locus.Dqb1, LocusPosition.One, searchHlas.Dqb1.Position1)
                .WithLocusSearchHla(Locus.Dqb1, LocusPosition.Two, searchHlas.Dqb1.Position2)
                .WithLocusSearchHla(Locus.Drb1, LocusPosition.One, searchHlas.Drb1.Position1)
                .WithLocusSearchHla(Locus.Drb1, LocusPosition.Two, searchHlas.Drb1.Position2);

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
            searchRequestBuilder = searchRequestBuilder
                .WithTotalMismatchCount(1);
            return this;
        }

        public SearchRequestFromHlasBuilder FourOutOfSix()
        {
            searchRequestBuilder = searchRequestBuilder
                .WithTotalMismatchCount(2);
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

        public SearchRequestFromHlasBuilder WithDpb1ExcludedFromScoringAggregation()
        {
            searchRequestBuilder.WithLociExcludedFromScoringAggregates(new List<Locus> { Locus.Dpb1 });
            return this;
        }

        public SearchRequestFromHlasBuilder WithEmptyLocusSearchHlaAt(Locus locus)
        {
            searchRequestBuilder.WithEmptyLocusSearchHlaAt(locus);
            return this;
        }

        public SearchRequestFromHlasBuilder WithNullLocusSearchHlasAt(Locus locus)
        {
            searchRequestBuilder.WithLocusSearchHla(locus, LocusPosition.One, null);
            searchRequestBuilder.WithLocusSearchHla(locus, LocusPosition.Two, null);
            return this;
        }

        public SearchRequest Build()
        {
            return searchRequestBuilder.Build();
        }
    }
}