using System;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Common.Models;

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
                .WithLocusMatchHla(Locus.A, TypePosition.One, searchHlas.A_1)
                .WithLocusMatchHla(Locus.A, TypePosition.Two, searchHlas.A_2)
                .WithLocusMatchHla(Locus.B, TypePosition.One, searchHlas.B_1)
                .WithLocusMatchHla(Locus.B, TypePosition.Two, searchHlas.B_2)
                .WithLocusMatchHla(Locus.C, TypePosition.One, searchHlas.C_1)
                .WithLocusMatchHla(Locus.C, TypePosition.Two, searchHlas.C_2)
                .WithLocusMatchHla(Locus.Dqb1, TypePosition.One, searchHlas.DQB1_1)
                .WithLocusMatchHla(Locus.Dqb1, TypePosition.Two, searchHlas.DQB1_2)
                .WithLocusMatchHla(Locus.Drb1, TypePosition.One, searchHlas.DRB1_1)
                .WithLocusMatchHla(Locus.Drb1, TypePosition.Two, searchHlas.DRB1_2);

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
                .WithLocusMatchHla(locus, TypePosition.One, nonMatchingHlas.DataAtLocus(locus).Item1);
            return this;
        }

        public SearchRequest Build()
        {
            return searchRequestBuilder.Build();
        }
    }
}
