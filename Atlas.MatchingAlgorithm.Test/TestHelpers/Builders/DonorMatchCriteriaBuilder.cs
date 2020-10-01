using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    public class DonorMatchCriteriaBuilder
    {
        private readonly AlleleLevelMatchCriteria request = new AlleleLevelMatchCriteria()
        {
            LocusCriteria = new LociInfo<AlleleLevelLocusMatchCriteria>(),
            SearchType = DonorType.Adult
        };

        public DonorMatchCriteriaBuilder WithDonorMismatchCount(int count)
        {
            request.DonorMismatchCount = count;
            return this;
        }

        public DonorMatchCriteriaBuilder WithLocusMismatchA(int mismatchCount) => WithLocusMismatchCount(Locus.A, mismatchCount);
        public DonorMatchCriteriaBuilder WithLocusMismatchB(int mismatchCount) => WithLocusMismatchCount(Locus.B, mismatchCount);
        public DonorMatchCriteriaBuilder WithLocusMismatchDrb1(int mismatchCount) => WithLocusMismatchCount(Locus.Drb1, mismatchCount);

        public DonorMatchCriteriaBuilder WithLocusMismatchA(string hla1, string hla2, int mismatchCount) =>
            WithLocusMismatchA(new List<string> {hla1}, new List<string> {hla2}, mismatchCount);

        public DonorMatchCriteriaBuilder WithLocusMismatchB(string hla1, string hla2, int mismatchCount) =>
            WithLocusMismatchB(new List<string> {hla1}, new List<string> {hla2}, mismatchCount);

        public DonorMatchCriteriaBuilder WithLocusMismatchDrb1(string hla1, string hla2, int mismatchCount) =>
            WithLocusMismatchDrb1(new List<string> {hla1}, new List<string> {hla2}, mismatchCount);

        public DonorMatchCriteriaBuilder WithLocusMismatchA(IEnumerable<string> hla1, IEnumerable<string> hla2, int mismatchCount) =>
            WithLocusHlaAndMismatchCount(Locus.A, hla1, hla2, mismatchCount);

        public DonorMatchCriteriaBuilder WithLocusMismatchB(IEnumerable<string> hla1, IEnumerable<string> hla2, int mismatchCount) =>
            WithLocusHlaAndMismatchCount(Locus.B, hla1, hla2, mismatchCount);

        public DonorMatchCriteriaBuilder WithLocusMismatchDrb1(IEnumerable<string> hla1, IEnumerable<string> hla2, int mismatchCount) =>
            WithLocusHlaAndMismatchCount(Locus.Drb1, hla1, hla2, mismatchCount);

        public DonorMatchCriteriaBuilder WithLocusMismatchCount(Locus locus, int mismatchCount)
        {
            var criteria = request.LocusCriteria.GetLocus(locus) ?? new AlleleLevelLocusMatchCriteria();
            criteria.MismatchCount = mismatchCount;
            request.LocusCriteria = request.LocusCriteria.SetLocus(locus, criteria);
            return this;
        }

        public DonorMatchCriteriaBuilder WithNoCriteriaAtLocus(Locus locus)
        {
            request.LocusCriteria = request.LocusCriteria.SetLocus(locus, null);
            return this;
        }
        
        public DonorMatchCriteriaBuilder WithLocusHlaAndMismatchCount(
            Locus locus,
            IEnumerable<string> hla1,
            IEnumerable<string> hla2,
            int mismatchCount)
        {
            request.LocusCriteria = request.LocusCriteria.SetLocus(locus, new AlleleLevelLocusMatchCriteria
            {
                PGroupsToMatchInPositionOne = hla1,
                PGroupsToMatchInPositionTwo = hla2,
                MismatchCount = mismatchCount
            });
            return this;
        }

        internal AlleleLevelMatchCriteria Build() => request;
    }
}