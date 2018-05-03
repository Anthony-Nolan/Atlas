using Nova.SearchAlgorithm.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.Builders
{
    public class SearchRequestBuilder
    {
        private SearchRequest request = new SearchRequest()
        {
            RegistriesToSearch = new List<RegistryCode> {
                RegistryCode.AN, RegistryCode.DKMS, RegistryCode.FRANCE, RegistryCode.NHSBT, RegistryCode.NMDP, RegistryCode.WBS
            },
            SearchType = SearchType.Adult,
            MatchCriteria = new MismatchCriteria()
        };

        public SearchRequestBuilder WithDonorMismatchCount(int count)
        {
            request.MatchCriteria.DonorMismatchCount = count;

            return this;
        }

        public SearchRequestBuilder WithLocusMismatchA(string hla1, string hla2, int mismatchCount)
        {
            request.MatchCriteria.LocusMismatchA = new LocusMismatchCriteria
            {
                SearchHla1 = hla1,
                SearchHla2 = hla2,
                MismatchCount = mismatchCount,
            };
            return this;
        }

        public SearchRequest Build()
        {
            return request;
        }
    }
}
