using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.Builders
{
    public class DonorMatchCriteriaBuilder
    {
        private DonorMatchCriteria request = new DonorMatchCriteria()
        {
            RegistriesToSearch = new List<RegistryCode> {
                RegistryCode.AN, RegistryCode.DKMS, RegistryCode.FRANCE, RegistryCode.NHSBT, RegistryCode.NMDP, RegistryCode.WBS
            },
            SearchType = SearchType.Adult
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
            request.LocusMismatchA = new DonorLocusMatchCriteria
            {
                HlaNamesToMatchInPositionOne = hla1,
                HlaNamesToMatchInPositionTwo = hla2,
                MismatchCount = mismatchCount,
            };
            return this;
        }
        public DonorMatchCriteriaBuilder WithLocusMismatchB(IEnumerable<string> hla1, IEnumerable<string> hla2, int mismatchCount)
        {
            request.LocusMismatchB = new DonorLocusMatchCriteria
            {
                HlaNamesToMatchInPositionOne = hla1,
                HlaNamesToMatchInPositionTwo = hla2,
                MismatchCount = mismatchCount,
            };
            return this;
        }

        public DonorMatchCriteriaBuilder WithLocusMismatchDRB1(IEnumerable<string> hla1, IEnumerable<string> hla2, int mismatchCount)
        {
            request.LocusMismatchDRB1 = new DonorLocusMatchCriteria
            {
                HlaNamesToMatchInPositionOne = hla1,
                HlaNamesToMatchInPositionTwo = hla2,
                MismatchCount = mismatchCount,
            };
            return this;
        }

        public DonorMatchCriteria Build()
        {
            return request;
        }
    }
}
