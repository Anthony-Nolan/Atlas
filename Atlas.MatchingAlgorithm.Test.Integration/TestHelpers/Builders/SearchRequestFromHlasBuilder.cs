using System;
using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders
{
    /// <summary>
    /// Build search requests from the submitted HLA typings.
    /// The typings at specified loci positions can be easily altered to induce mismatched scenarios.
    /// </summary>
    public class SearchRequestFromHlasBuilder
    {
        private readonly PhenotypeInfo<string> nonMatchingHlas;
        private MatchingRequestBuilder matchingRequestBuilder;

        /// <param name="searchHlas">A selection of valid HLA data.</param>
        /// <param name="nonMatchingHlas">A selection of valid hla strings that do not match the search HLA.</param>
        public SearchRequestFromHlasBuilder(PhenotypeInfo<string> searchHlas, PhenotypeInfo<string> nonMatchingHlas = null)
        {
            matchingRequestBuilder = new MatchingRequestBuilder()
                .WithSearchType(DonorType.Adult)
                .WithLociExcludedFromScoringAggregates(new List<Locus>())
                .WithSearchHla(searchHlas);
            this.nonMatchingHlas = nonMatchingHlas;
        }

        public SearchRequestFromHlasBuilder TenOutOfTen()
        {
            matchingRequestBuilder = matchingRequestBuilder
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
            matchingRequestBuilder = matchingRequestBuilder
                .WithTotalMismatchCount(0)
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0);
            return this;
        }

        public SearchRequestFromHlasBuilder FiveOutOfSix()
        {
            matchingRequestBuilder = matchingRequestBuilder.WithTotalMismatchCount(1);
            return this;
        }

        public SearchRequestFromHlasBuilder FourOutOfSix()
        {
            matchingRequestBuilder = matchingRequestBuilder.WithTotalMismatchCount(2);
            return this;
        }

        public SearchRequestFromHlasBuilder WithSingleMismatchRequestedAt(Locus locus)
        {
            matchingRequestBuilder = matchingRequestBuilder
                .WithLocusMismatchCount(Locus.A, locus == Locus.A ? 1 : 0)
                .WithLocusMismatchCount(Locus.B, locus == Locus.B ? 1 : 0)
                .WithLocusMismatchCount(Locus.Drb1, locus == Locus.Drb1 ? 1 : 0);
            return this;
        }

        public SearchRequestFromHlasBuilder WithDoubleMismatchRequestedAt(Locus locus)
        {
            matchingRequestBuilder = matchingRequestBuilder
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

            matchingRequestBuilder = matchingRequestBuilder
                .WithLocusSearchHla(locus, LocusPosition.One, nonMatchingHlas.GetLocus(locus).Position1);
            return this;
        }

        public SearchRequestFromHlasBuilder WithDpb1ExcludedFromScoringAggregation()
        {
            matchingRequestBuilder.WithLociExcludedFromScoringAggregates(new List<Locus> {Locus.Dpb1});
            return this;
        }

        public SearchRequestFromHlasBuilder WithNullLocusSearchHlasAt(Locus locus)
        {
            matchingRequestBuilder.WithNullLocusSearchHla(locus);
            return this;
        }

        public MatchingRequest Build()
        {
            return matchingRequestBuilder.Build();
        }
    }
}