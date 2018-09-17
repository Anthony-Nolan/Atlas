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
        public SearchRequestFromHlasBuilder(PhenotypeInfo<string> searchHlas, PhenotypeInfo<string> nonMatchingHlas)
        {
            searchRequestBuilder = new SearchRequestBuilder()
                .WithLocusMatchHla(Locus.A, TypePositions.One, searchHlas.A_1)
                .WithLocusMatchHla(Locus.A, TypePositions.Two, searchHlas.A_2)
                .WithLocusMatchHla(Locus.B, TypePositions.One, searchHlas.B_1)
                .WithLocusMatchHla(Locus.B, TypePositions.Two, searchHlas.B_2)
                .WithLocusMatchHla(Locus.C, TypePositions.One, searchHlas.C_1)
                .WithLocusMatchHla(Locus.C, TypePositions.Two, searchHlas.C_2)
                .WithLocusMatchHla(Locus.Dqb1, TypePositions.One, searchHlas.DQB1_1)
                .WithLocusMatchHla(Locus.Dqb1, TypePositions.Two, searchHlas.DQB1_2)
                .WithLocusMatchHla(Locus.Drb1, TypePositions.One, searchHlas.DRB1_1)
                .WithLocusMatchHla(Locus.Drb1, TypePositions.Two, searchHlas.DRB1_2);

            this.nonMatchingHlas = nonMatchingHlas;
        }

        /// <summary>
        /// Builds search request from HLAs without setting the non-matching HLA phenotype.
        /// Do not use for mismatch searches.
        /// </summary>
        public static SearchRequestFromHlasBuilder WithoutNonMatchingHlas(PhenotypeInfo<string> searchHlas)
        {
            return new SearchRequestFromHlasBuilder(searchHlas, null);
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

        public SearchRequestFromHlasBuilder WithSingleMismatchRequestedAt(Locus locus)
        {
            searchRequestBuilder = searchRequestBuilder
                .WithLocusMismatchCount(Locus.A, locus == Locus.A ? 1 : 0)
                .WithLocusMismatchCount(Locus.B, locus == Locus.B ? 1 : 0)
                .WithLocusMismatchCount(Locus.Drb1, locus == Locus.Drb1 ? 1 : 0);
            return this;
        }

        public SearchRequestFromHlasBuilder WithPositionOneOfSearchHlaMismatchedAt(Locus locus)
        {
            if (nonMatchingHlas == null)
            {
                throw new Exception("Non-matching HLA data has not been provided.");
            }

            searchRequestBuilder = searchRequestBuilder
                .WithLocusMatchHla(locus, TypePositions.One, nonMatchingHlas.DataAtLocus(locus).Item1);
            return this;
        }

        public SearchRequest Build()
        {
            return searchRequestBuilder.Build();
        }
    }
}
