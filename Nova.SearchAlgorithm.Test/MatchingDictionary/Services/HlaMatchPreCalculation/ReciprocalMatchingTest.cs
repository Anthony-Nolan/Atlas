using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using NUnit.Framework;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation
{
    public class ReciprocalMatchingTest : MatchedOnTestBase<IMatchedHla>
    {
        [TestCase(MatchLocus.A, "02:01:100", "2")]
        [TestCase(MatchLocus.B, "39:55", "39")]
        [TestCase(MatchLocus.C, "01:80", "1")]
        [TestCase(MatchLocus.Dqb1, "03:01:15", "7")]
        [TestCase(MatchLocus.Drb1, "04:155", "4")]
        public void HlaMatchPrecalculation_AlleleAndSerologyTypingsMatchReciprocally(
            MatchLocus matchLocus,
            string alleleName,
            string serologyName)
        {
            var allele = GetSingleMatchingTyping(matchLocus, alleleName);
            var serology = GetSingleMatchingTyping(matchLocus, serologyName);

            allele.MatchingSerologies
                .Select(ser => ser.SerologyTyping as HlaTyping)
                .Should()
                .Contain(serology.HlaTyping);

            serology.MatchingPGroups
                .Should()
                .IntersectWith(allele.MatchingPGroups);
        }
    }
}
