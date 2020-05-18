﻿using FluentAssertions;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.MatchingTypings;
using NUnit.Framework;
using System.Linq;
using Atlas.Utils.Models;

namespace Atlas.MatchingAlgorithm.Test.HlaMetadataDictionary.Services.HlaMatchPreCalculation
{
    public class ReciprocalMatchingTest : MatchedOnTestBase<IMatchedHla>
    {
        [TestCase(Locus.A, "02:01:100", "2")]
        [TestCase(Locus.B, "39:55", "39")]
        [TestCase(Locus.C, "01:80", "1")]
        [TestCase(Locus.Dqb1, "03:01:15", "7")]
        [TestCase(Locus.Drb1, "04:155", "4")]
        public void HlaMatchPrecalculation_AlleleAndSerologyTypingsMatchReciprocally(
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
