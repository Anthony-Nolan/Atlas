using System;
using System.Collections.Generic;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Common.Models;
using Nova.Utils.Models;
using Locus = Nova.SearchAlgorithm.Common.Models.Locus;

namespace Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders
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
                .ForRegistries(new List<RegistryCode> { RegistryCode.AN })
                .WithLociExcludedFromScoringAggregates(new List<LocusType>())
                .WithLocusSearchHla(Locus.A, TypePosition.One, searchHlas.A.Position1)
                .WithLocusSearchHla(Locus.A, TypePosition.Two, searchHlas.A.Position2)
                .WithLocusSearchHla(Locus.B, TypePosition.One, searchHlas.B.Position1)
                .WithLocusSearchHla(Locus.B, TypePosition.Two, searchHlas.B.Position2)
                .WithLocusSearchHla(Locus.C, TypePosition.One, searchHlas.C.Position1)
                .WithLocusSearchHla(Locus.C, TypePosition.Two, searchHlas.C.Position2)
                .WithLocusSearchHla(Locus.Dpb1, TypePosition.One, searchHlas.Dpb1.Position1)
                .WithLocusSearchHla(Locus.Dpb1, TypePosition.Two, searchHlas.Dpb1.Position2)
                .WithLocusSearchHla(Locus.Dqb1, TypePosition.One, searchHlas.Dqb1.Position1)
                .WithLocusSearchHla(Locus.Dqb1, TypePosition.Two, searchHlas.Dqb1.Position2)
                .WithLocusSearchHla(Locus.Drb1, TypePosition.One, searchHlas.Drb1.Position1)
                .WithLocusSearchHla(Locus.Drb1, TypePosition.Two, searchHlas.Drb1.Position2);

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
                .WithLocusSearchHla(locus, TypePosition.One, nonMatchingHlas.DataAtLocus(locus).Item1);
            return this;
        }

        public SearchRequestFromHlasBuilder WithDpb1ExcludedFromScoringAggregation()
        {
            searchRequestBuilder.WithLociExcludedFromScoringAggregates(new List<LocusType> { LocusType.Dpb1 });
            return this;
        }

        public SearchRequestFromHlasBuilder WithEmptyLocusSearchHlaAt(Locus locus)
        {
            searchRequestBuilder.WithEmptyLocusSearchHlaAt(locus);
            return this;
        }

        public SearchRequestFromHlasBuilder WithNullLocusSearchHlasAt(Locus locus)
        {
            searchRequestBuilder.WithLocusSearchHla(locus, TypePosition.One, null);
            searchRequestBuilder.WithLocusSearchHla(locus, TypePosition.Two, null);
            return this;
        }

        public SearchRequest Build()
        {
            return searchRequestBuilder.Build();
        }
    }
}