using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests
{
    public class SearchRequestBuilder
    {
        private readonly SearchRequest searchRequest;
        private PhenotypeInfo<string> searchHla;
        private LociInfo<int?> locusMismatchCounts;

        public SearchRequestBuilder()
        {
            searchRequest = new SearchRequest()
            {
                SearchDonorType = Atlas.Client.Models.Search.DonorType.Adult,
                MatchCriteria = new MismatchCriteria
                {
                    DonorMismatchCount = 0
                },
                ScoringCriteria = new ScoringCriteria()
                {
                    LociToScore = new List<Locus>(),
                    LociToExcludeFromAggregateScore = new List<Locus>()
                }
            };

            searchHla = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>("default-hla-a"),
                valueB: new LocusInfo<string>("default-hla-b"),
                valueDrb1: new LocusInfo<string>("default-hla-drb1")
            );

            locusMismatchCounts = new LociInfo<int?>(valueA: 0, valueB: 0, valueDrb1: 0, valueC: null, valueDqb1: null);
        }

        #region Match Criteria

        public SearchRequestBuilder WithTotalMismatchCount(int mismatchCount)
        {
            searchRequest.MatchCriteria.DonorMismatchCount = mismatchCount;
            return this;
        }

        public SearchRequestBuilder WithLocusMismatchCount(Locus locus, int? locusMismatchCount)
        {
            if (locus == Locus.Dpb1)
            {
                throw new NotImplementedException();
            }

            locusMismatchCounts = locusMismatchCounts.SetLocus(locus, locusMismatchCount);
            return this;
        }

        public SearchRequestBuilder WithMismatchCountAtLoci(IEnumerable<Locus> loci, int locusMismatchCount)
        {
            return loci.Aggregate(this, (current, locus) => current.WithLocusMismatchCount(locus, locusMismatchCount));
        }

        #endregion

        #region Scoring Criteria

        public SearchRequestBuilder WithLociToScore(IEnumerable<Locus> loci)
        {
            searchRequest.ScoringCriteria.LociToScore = loci.ToList();
            return this;
        }

        public SearchRequestBuilder WithLociExcludedFromScoringAggregates(IEnumerable<Locus> loci)
        {
            searchRequest.ScoringCriteria.LociToExcludeFromAggregateScore = loci.ToList();
            return this;
        }

        #endregion

        #region Patient Hla

        private SearchRequestBuilder WithNullLocusSearchHla(Locus locus, LocusPosition position)
        {
            searchHla = searchHla.SetPosition(locus, position, null);
            return this;
        }

        public SearchRequestBuilder WithNullLocusSearchHla(Locus locus)
        {
            return WithNullLocusSearchHla(locus, LocusPosition.One)
                .WithNullLocusSearchHla(locus, LocusPosition.Two);
        }

        /// <summary>
        /// Sets the HLA at given locus/position, ignoring any nulls. To explicitly set nulls within a non-empty LocusInfo, use <see cref="WithNullLocusSearchHla"/> 
        /// </summary>
        public SearchRequestBuilder WithLocusSearchHla(Locus locus, LocusPosition position, string hlaString)
        {
            // API level validation will fail if individual hla are null, but not if the locus is omitted altogether. 
            // If tests need to be added which set individual values to null (i.e. to test that validation), another builder method should be used
            if (hlaString == null)
            {
                return this;
            }

            // If locus is specified, but currently null, initialise that locus. 
            searchHla = new PhenotypeInfo<string>(searchHla.Map((l, hla) =>
                {
                    if (l == locus)
                    {
                        return hla ?? new LocusInfo<string>();
                    }

                    return hla;
                }))
                .SetPosition(locus, position, hlaString);

            return this;
        }

        public SearchRequestBuilder WithSearchHla(PhenotypeInfo<string> newSearchHla)
        {
            searchHla = newSearchHla;
            return this;
        }

        #endregion

        public SearchRequestBuilder WithSearchType(DonorType donorType)
        {
            searchRequest.SearchDonorType = donorType.ToAtlasClientModel();
            return this;
        }

        public SearchRequestBuilder WithBetterMatchesConfig(bool shouldBetterMatchesBeAllowed)
        {
            searchRequest.MatchCriteria.IncludeBetterMatches = shouldBetterMatchesBeAllowed;
            return this;
        }

        public SearchRequestBuilder WithDonorRegistryCodes(List<string> registryCodes)
        {
            searchRequest.DonorRegistryCodes = registryCodes;
            return this;
        }

        public SearchRequest Build()
        {
            searchRequest.SearchHlaData = searchHla.ToPhenotypeInfoTransfer();
            searchRequest.MatchCriteria.LocusMismatchCriteria = locusMismatchCounts.ToLociInfoTransfer();
            return searchRequest;
        }
    }
}