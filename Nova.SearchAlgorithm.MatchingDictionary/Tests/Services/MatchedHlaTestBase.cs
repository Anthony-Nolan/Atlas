using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.MatchingDictionary.Tests.Services
{
    public class MatchedHlaTestBase<TMatchedHla> where TMatchedHla : IMatchedHla
    {
        protected List<TMatchedHla> MatchingTypes;

        public MatchedHlaTestBase(IEnumerable<TMatchedHla> matchingTypes)
        {
            MatchingTypes = matchingTypes.ToList();
        }

        protected TMatchedHla GetSingleMatchingType(string matchLocus, string hlaName)
        {
            return MatchingTypes.Single(m =>
                m.HlaType.MatchLocus.Equals(matchLocus) && m.HlaType.Name.Equals(hlaName));
        }
    }
}
