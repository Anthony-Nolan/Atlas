using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching
{
    [TestFixtureSource(typeof(MatchedHlaTestFixtureArgs), nameof(MatchedHlaTestFixtureArgs.MatchedHla))]
    public class ReciprocalMatchingTest : MatchedOnTestBase<IMatchedHla>
    {
        public ReciprocalMatchingTest(IEnumerable<IMatchedHla> matchingTypes) : base(matchingTypes)
        {
        }

        [Test]
        public void HlaTypesMatchReciprocally()
        {
            var alleleA = GetSingleMatchingType(MatchLocus.A, "02:01:100");
            var serologyA = GetSingleMatchingType(MatchLocus.A, "2");

            var alleleB = GetSingleMatchingType(MatchLocus.B, "39:55");
            var serologyB = GetSingleMatchingType(MatchLocus.B, "39");

            var alleleC = GetSingleMatchingType(MatchLocus.C, "01:80");
            var serologyC = GetSingleMatchingType(MatchLocus.C, "1");

            var alleleDqb1 = GetSingleMatchingType(MatchLocus.Dqb1, "03:01:15");
            var serologyDqb1 = GetSingleMatchingType(MatchLocus.Dqb1, "7");

            var alleleDrb1 = GetSingleMatchingType(MatchLocus.Drb1, "04:155");
            var serologyDrb1 = GetSingleMatchingType(MatchLocus.Drb1, "4");

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
