using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatching
{
    [TestFixtureSource(typeof(HlaMatchingTestFixtureArgs), "MatchedHla")]
    public class ReciprocalMatchingTest : MatchedOnTestBase<IMatchedHla>
    {
        public ReciprocalMatchingTest(IEnumerable<IMatchedHla> matchingTypes) : base(matchingTypes)
        {
        }

        [Test]
        public void HlaTypesMatchReciprocally()
        {
            var alleleA = GetSingleMatchingType("A", "02:01:100");
            var serologyA = GetSingleMatchingType("A", "2");

            var alleleB = GetSingleMatchingType("B", "39:55");
            var serologyB = GetSingleMatchingType("B", "39");

            var alleleC = GetSingleMatchingType("C", "01:80");
            var serologyC = GetSingleMatchingType("C", "1");

            var alleleDqb1 = GetSingleMatchingType("DQB1", "03:01:15");
            var serologyDqb1 = GetSingleMatchingType("DQB1", "7");

            var alleleDrb1 = GetSingleMatchingType("DRB1", "04:155");
            var serologyDrb1 = GetSingleMatchingType("DRB1", "4");

            IsReciprocallyMatchedTest(alleleA, serologyA);
            IsReciprocallyMatchedTest(alleleB, serologyB);
            IsReciprocallyMatchedTest(alleleC, serologyC);
            IsReciprocallyMatchedTest(alleleDqb1, serologyDqb1);
            IsReciprocallyMatchedTest(alleleDrb1, serologyDrb1);
        }

        private static void IsReciprocallyMatchedTest(IMatchedHla allele, IMatchedHla serology)
        {
            Assert.IsTrue(allele.MatchingSerologies.Contains(serology.HlaType));
            Assert.IsTrue(serology.MatchingPGroups.Intersect(allele.MatchingPGroups).Any());
        }
    }
}
