using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    public class SearchRequestBuilder
    {
        private readonly MatchingRequest matchingRequest;

        public SearchRequestBuilder()
        {
            matchingRequest = new MatchingRequest
            {
                SearchType = DonorType.Adult,
                MatchCriteria = new MismatchCriteria
                {
                    LocusMismatchA = new LocusMismatchCriteria(),
                    LocusMismatchB = new LocusMismatchCriteria(),
                    LocusMismatchDrb1 = new LocusMismatchCriteria(),
                    LocusMismatchC = null,
                    LocusMismatchDqb1 = null
                },
                SearchHlaData = new PhenotypeInfo<string>
                {
                    A = new LocusInfo<string>(),
                    B = new LocusInfo<string>(),
                    Drb1 = new LocusInfo<string>(),
                    C = null,
                    Dpb1 = null,
                    Dqb1 = null
                },
                LociToExcludeFromAggregateScore = new List<Locus>()
            };
        }

        #region Match Criteria

        public SearchRequestBuilder WithTotalMismatchCount(int mismatchCount)
        {
            matchingRequest.MatchCriteria.DonorMismatchCount = mismatchCount;
            return this;
        }

        // TODO: ATLAS-236: PhenotypeInfo for matching criteria

        public SearchRequestBuilder WithLocusMismatchCount(Locus locus, int locusMismatchCount)
        {
            switch (locus)
            {
                case Locus.A:
                    matchingRequest.MatchCriteria.LocusMismatchA ??= new LocusMismatchCriteria();
                    matchingRequest.MatchCriteria.LocusMismatchA.MismatchCount = locusMismatchCount;
                    break;
                case Locus.B:
                    matchingRequest.MatchCriteria.LocusMismatchB ??= new LocusMismatchCriteria();
                    matchingRequest.MatchCriteria.LocusMismatchB.MismatchCount = locusMismatchCount;
                    break;
                case Locus.C:
                    matchingRequest.MatchCriteria.LocusMismatchC ??= new LocusMismatchCriteria();
                    matchingRequest.MatchCriteria.LocusMismatchC.MismatchCount = locusMismatchCount;
                    break;
                case Locus.Dpb1:
                    throw new NotImplementedException();
                case Locus.Dqb1:
                    matchingRequest.MatchCriteria.LocusMismatchDqb1 ??= new LocusMismatchCriteria();
                    matchingRequest.MatchCriteria.LocusMismatchDqb1.MismatchCount = locusMismatchCount;
                    break;
                case Locus.Drb1:
                    matchingRequest.MatchCriteria.LocusMismatchDrb1 ??= new LocusMismatchCriteria();
                    matchingRequest.MatchCriteria.LocusMismatchDrb1.MismatchCount = locusMismatchCount;
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

        #endregion

        #region Patient Hla

        private SearchRequestBuilder WithNullLocusSearchHla(Locus locus, LocusPosition position)
        {
            matchingRequest.SearchHlaData.SetPosition(locus, position, null);
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
            matchingRequest.SearchHlaData = new PhenotypeInfo<string>(matchingRequest.SearchHlaData.Map((l, hla) =>
            {
                if (l == locus)
                {
                    return hla ?? new LocusInfo<string>();
                }

                return hla;
            }));

            matchingRequest.SearchHlaData.SetPosition(locus, position, hlaString);
            return this;
        }

        public SearchRequestBuilder WithEmptyLocusSearchHlaAt(Locus locus)
        {
            matchingRequest.SearchHlaData.SetLocus(locus, (LocusInfo<string>) null);
            return this;
        }

        public SearchRequestBuilder WithSearchHla(PhenotypeInfo<string> searchHla)
        {
            matchingRequest.SearchHlaData = searchHla;
            return this;
        }

        #endregion

        public SearchRequestBuilder WithSearchType(DonorType donorType)
        {
            matchingRequest.SearchType = donorType;
            return this;
        }

        public SearchRequestBuilder WithLociExcludedFromScoringAggregates(IEnumerable<Locus> loci)
        {
            matchingRequest.LociToExcludeFromAggregateScore = loci;
            return this;
        }

        public MatchingRequest Build()
        {
            return matchingRequest;
        }
    }
}