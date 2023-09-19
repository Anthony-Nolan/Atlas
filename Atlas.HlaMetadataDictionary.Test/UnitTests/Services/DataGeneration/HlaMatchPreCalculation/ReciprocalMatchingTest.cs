using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataGeneration.HlaMatchPreCalculation
{
    internal class ReciprocalMatchingTest : MatchedOnTestBase<IMatchedHla>
    {
        [TestCase(Locus.A, "02:01:100", "2")]
        [TestCase(Locus.B, "39:55", "39")]
        [TestCase(Locus.C, "01:80", "1")]
        [TestCase(Locus.Dqb1, "03:01:15", "7")]
        [TestCase(Locus.Drb1, "04:155", "4")]
        public void HlaMatchPreCalculation_AlleleAndSerologyTypingsMatchReciprocally(
            Locus locus,
            string alleleName,
            string serologyName)
        {
            var allele = GetSingleMatchingTyping(locus, alleleName);
            var serology = GetSingleMatchingTyping(locus, serologyName);

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
