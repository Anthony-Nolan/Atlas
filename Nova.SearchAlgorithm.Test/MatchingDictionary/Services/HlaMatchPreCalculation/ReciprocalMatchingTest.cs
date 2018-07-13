using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation
{
    [TestFixtureSource(typeof(MatchedHlaTestFixtureArgs), nameof(MatchedHlaTestFixtureArgs.MatchedHla))]
    public class ReciprocalMatchingTest : MatchedOnTestBase<IMatchedHla>
    {
        public ReciprocalMatchingTest(IEnumerable<IMatchedHla> matchingTypes) : base(matchingTypes)
        {
        }

        [Test]
        public void HlaTypingsMatchReciprocally()
        {
            var alleleA = GetSingleMatchingTyping(MatchLocus.A, "02:01:100");
            var serologyA = GetSingleMatchingTyping(MatchLocus.A, "2");

            var alleleB = GetSingleMatchingTyping(MatchLocus.B, "39:55");
            var serologyB = GetSingleMatchingTyping(MatchLocus.B, "39");

            var alleleC = GetSingleMatchingTyping(MatchLocus.C, "01:80");
            var serologyC = GetSingleMatchingTyping(MatchLocus.C, "1");

            var alleleDqb1 = GetSingleMatchingTyping(MatchLocus.Dqb1, "03:01:15");
            var serologyDqb1 = GetSingleMatchingTyping(MatchLocus.Dqb1, "7");

            var alleleDrb1 = GetSingleMatchingTyping(MatchLocus.Drb1, "04:155");
            var serologyDrb1 = GetSingleMatchingTyping(MatchLocus.Drb1, "4");

            IsReciprocallyMatchedTest(alleleA, serologyA);
            IsReciprocallyMatchedTest(alleleB, serologyB);
            IsReciprocallyMatchedTest(alleleC, serologyC);
            IsReciprocallyMatchedTest(alleleDqb1, serologyDqb1);
            IsReciprocallyMatchedTest(alleleDrb1, serologyDrb1);
        }

        private static void IsReciprocallyMatchedTest(IMatchedHla allele, IMatchedHla serology)
        {
            Assert.IsTrue(allele.MatchingSerologies.Contains(serology.HlaTyping));
            Assert.IsTrue(serology.MatchingPGroups.Intersect(allele.MatchingPGroups).Any());
        }
    }
}
