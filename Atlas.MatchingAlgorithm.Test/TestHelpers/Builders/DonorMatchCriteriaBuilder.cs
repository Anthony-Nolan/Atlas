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

        public DonorMatchCriteriaBuilder WithLocusMismatchA(string hla1, string hla2, int mismatchCount) =>
            WithLocusMismatchA(new List<string> {hla1}, new List<string> {hla2}, mismatchCount);

        public DonorMatchCriteriaBuilder WithLocusMismatchB(string hla1, string hla2, int mismatchCount) =>
            WithLocusMismatchB(new List<string> {hla1}, new List<string> {hla2}, mismatchCount);

        public DonorMatchCriteriaBuilder WithLocusMismatchDRB1(string hla1, string hla2, int mismatchCount) =>
            WithLocusMismatchDRB1(new List<string> {hla1}, new List<string> {hla2}, mismatchCount);

        public DonorMatchCriteriaBuilder WithLocusMismatchA(IEnumerable<string> hla1, IEnumerable<string> hla2, int mismatchCount) =>
            WithLocusMismatch(Locus.A, hla1, hla2, mismatchCount);

        public DonorMatchCriteriaBuilder WithLocusMismatchB(IEnumerable<string> hla1, IEnumerable<string> hla2, int mismatchCount) =>
            WithLocusMismatch(Locus.B, hla1, hla2, mismatchCount);

        public DonorMatchCriteriaBuilder WithLocusMismatchDRB1(IEnumerable<string> hla1, IEnumerable<string> hla2, int mismatchCount) =>
            WithLocusMismatch(Locus.Drb1, hla1, hla2, mismatchCount);

        public DonorMatchCriteriaBuilder WithLocusMismatch(Locus locus, IEnumerable<string> hla1, IEnumerable<string> hla2, int mismatchCount)
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