using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders
{
    /// <summary>
    /// Builds a search request for matching on loci: A, B & DRB1.
    /// Allows HLA typings at specified loci positions to be easily altered
    /// to induce mismatched scenarios.
    /// </summary>
    public class ThreeLocusSearchRequestBuilder
    {
        private readonly PhenotypeInfo<string> nonMatchingHlas;
        private SearchRequestBuilder searchRequestBuilder;

        /// <param name="searchHlas">A selection of valid HLA data.</param>
        /// <param name="nonMatchingHlas">A selection of valid hla strings that do not match the search HLA.</param>
        public ThreeLocusSearchRequestBuilder(PhenotypeInfo<string> searchHlas, PhenotypeInfo<string> nonMatchingHlas)
        {
            this.nonMatchingHlas = nonMatchingHlas;
            searchRequestBuilder = new SearchRequestBuilder()
                .WithLocusMatchCriteria(Locus.A, new LocusMismatchCriteria
                {
                    MismatchCount = 0
                })
                .WithLocusMatchHla(Locus.A, TypePositions.One, searchHlas.A_1)
                .WithLocusMatchHla(Locus.A, TypePositions.Two, searchHlas.A_2)
                .WithLocusMatchCriteria(Locus.B, new LocusMismatchCriteria
                {
                    MismatchCount = 0
                })
                .WithLocusMatchHla(Locus.B, TypePositions.One, searchHlas.B_1)
                .WithLocusMatchHla(Locus.B, TypePositions.Two, searchHlas.B_2)
                .WithLocusMatchCriteria(Locus.Drb1, new LocusMismatchCriteria
                {
                    MismatchCount = 0
                })
                .WithLocusMatchHla(Locus.Drb1, TypePositions.One, searchHlas.DRB1_1)
                .WithLocusMatchHla(Locus.Drb1, TypePositions.Two, searchHlas.DRB1_2);
        }

        public ThreeLocusSearchRequestBuilder SixOutOfSix()
        {
            searchRequestBuilder = searchRequestBuilder
                .WithTotalMismatchCount(0)
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0);
            return this;
        }

        public ThreeLocusSearchRequestBuilder FiveOutOfSix()
        {
            searchRequestBuilder = searchRequestBuilder
                .WithTotalMismatchCount(1);
            return this;
        }

        public ThreeLocusSearchRequestBuilder WithSingleMismatchRequestedAt(Locus locus)
        {
            searchRequestBuilder = searchRequestBuilder
                .WithLocusMismatchCount(Locus.A, locus == Locus.A ? 1 : 0)
                .WithLocusMismatchCount(Locus.B, locus == Locus.B ? 1 : 0)
                .WithLocusMismatchCount(Locus.Drb1, locus == Locus.Drb1 ? 1 : 0);
            return this;
        }

        public ThreeLocusSearchRequestBuilder WithPositionOneOfSearchHlaMismatchedAt(Locus locus)
        {
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
