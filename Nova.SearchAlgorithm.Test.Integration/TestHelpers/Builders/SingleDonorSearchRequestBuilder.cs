using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders
{
    public class SingleDonorSearchRequestBuilder
    {
        private readonly PhenotypeInfo<string> nonMatchingHlas;
        private SearchRequestBuilder searchRequestBuilder;

        /// <param name="donorHlas">A selection of valid hla data for the single donor to have.</param>
        /// <param name="nonMatchingHlas">A selection of valid hla strings that do not match the donor's.</param>
        public SingleDonorSearchRequestBuilder(PhenotypeInfo<string> donorHlas, PhenotypeInfo<string> nonMatchingHlas)
        {
            this.nonMatchingHlas = nonMatchingHlas;
            searchRequestBuilder = new SearchRequestBuilder()
                .WithLocusMatchCriteria(Locus.A, new LocusMismatchCriteria
                {
                    MismatchCount = 0
                })
                .WithLocusMatchHla(Locus.A, TypePositions.One, donorHlas.A_1)
                .WithLocusMatchHla(Locus.A, TypePositions.Two, donorHlas.A_2)
                .WithLocusMatchCriteria(Locus.B, new LocusMismatchCriteria
                {
                    MismatchCount = 0
                })
                .WithLocusMatchHla(Locus.B, TypePositions.One, donorHlas.B_1)
                .WithLocusMatchHla(Locus.B, TypePositions.Two, donorHlas.B_2)
                .WithLocusMatchCriteria(Locus.Drb1, new LocusMismatchCriteria
                {
                    MismatchCount = 0
                })
                .WithLocusMatchHla(Locus.Drb1, TypePositions.One, donorHlas.DRB1_1)
                .WithLocusMatchHla(Locus.Drb1, TypePositions.Two, donorHlas.DRB1_2);
        }

        public SingleDonorSearchRequestBuilder SixOutOfSix()
        {
            searchRequestBuilder = searchRequestBuilder
                .WithTotalMismatchCount(0)
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0);
            return this;
        }

        public SingleDonorSearchRequestBuilder FiveOutOfSixWithPositionOneMismatchAt(Locus locus)
        {
            searchRequestBuilder = searchRequestBuilder
                .WithTotalMismatchCount(0)
                .WithLocusMismatchCount(Locus.A, locus == Locus.A ? 1 : 0)
                .WithLocusMismatchCount(Locus.B, locus == Locus.B ? 1 : 0)
                .WithLocusMismatchCount(Locus.Drb1, locus == Locus.Drb1 ? 1 : 0)
                .WithLocusMatchHla(locus, TypePositions.One, nonMatchingHlas.DataAtLocus(locus).Item1);
            return this;
        }

        public SingleDonorSearchRequestBuilder WithSingleMismatchAt(Locus locus)
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
