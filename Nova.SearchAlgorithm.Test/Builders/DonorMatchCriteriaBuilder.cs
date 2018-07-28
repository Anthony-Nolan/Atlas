using System.Collections.Generic;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Builders
{
    public class DonorMatchCriteriaBuilder
    {
        private readonly AlleleLevelMatchCriteria request = new AlleleLevelMatchCriteria()
        {
            RegistriesToSearch = new List<RegistryCode> {
                RegistryCode.AN, RegistryCode.DKMS, RegistryCode.FRANCE, RegistryCode.NHSBT, RegistryCode.NMDP, RegistryCode.WBS
            },
            SearchType = DonorType.Adult
        };

        public DonorMatchCriteriaBuilder WithDonorMismatchCount(int count)
        {
            request.DonorMismatchCount = count;

            return this;
        }

        public DonorMatchCriteriaBuilder WithLocusMismatchA(string hla1, string hla2, int mismatchCount)
        {
            return WithLocusMismatchA(new List<string> { hla1 }, new List<string> { hla2 }, mismatchCount);
        }

        public DonorMatchCriteriaBuilder WithLocusMismatchB(string hla1, string hla2, int mismatchCount)
        {
            return WithLocusMismatchB(new List<string> { hla1 }, new List<string> { hla2 }, mismatchCount);
        }

        public DonorMatchCriteriaBuilder WithLocusMismatchDRB1(string hla1, string hla2, int mismatchCount)
        {
            return WithLocusMismatchDRB1(new List<string> { hla1 }, new List<string> { hla2 }, mismatchCount);
        }

        public DonorMatchCriteriaBuilder WithLocusMismatchA(IEnumerable<string> hla1, IEnumerable<string> hla2, int mismatchCount)
        {
            request.LocusMismatchA = new AlleleLevelLocusMatchCriteria
            {
                PGroupsToMatchInPositionOne = hla1,
                PGroupsToMatchInPositionTwo = hla2,
                MismatchCount = mismatchCount,
            };
            return this;
        }
        public DonorMatchCriteriaBuilder WithLocusMismatchB(IEnumerable<string> hla1, IEnumerable<string> hla2, int mismatchCount)
        {
            request.LocusMismatchB = new AlleleLevelLocusMatchCriteria
            {
                PGroupsToMatchInPositionOne = hla1,
                PGroupsToMatchInPositionTwo = hla2,
                MismatchCount = mismatchCount,
            };
            return this;
        }

        public DonorMatchCriteriaBuilder WithLocusMismatchDRB1(IEnumerable<string> hla1, IEnumerable<string> hla2, int mismatchCount)
        {
            request.LocusMismatchDRB1 = new AlleleLevelLocusMatchCriteria
            {
                PGroupsToMatchInPositionOne = hla1,
                PGroupsToMatchInPositionTwo = hla2,
                MismatchCount = mismatchCount,
            };
            return this;
        }

        public AlleleLevelMatchCriteria Build()
        {
            return request;
        }
    }
}
