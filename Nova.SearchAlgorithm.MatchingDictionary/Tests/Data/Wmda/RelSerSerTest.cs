using System;
using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.MatchingDictionary.Tests.Data.Wmda
{
    [TestFixtureSource(typeof(SerologyTestFixtureArgs), "Args")]
    public class RelSerSerTest : WmdaDataTestBase<RelSerSer>
    {
        public RelSerSerTest(Func<IWmdaHlaType, bool> filter, IEnumerable<string> matchLoci)
            : base(filter, matchLoci)
        {
        }

        [Test]
        public void RelSerSerRegexCapturesRelationshipAsExpected()
        {
            var broadNoAssociated = new RelSerSer("A", "9", new List<string> { "23", "24" }, new List<string>());
            var broadWithAssociated = new RelSerSer("B", "21", new List<string> { "49", "50" }, new List<string> { "4005" });
            var splitWithAssociated = new RelSerSer("B", "51", new List<string>(), new List<string> { "5102", "5103" });
            var notSplitWithAssociated = new RelSerSer("DR", "14", new List<string>(), new List<string> { "1403", "1404" });

            Assert.AreEqual(broadNoAssociated, GetSingleWmdaHlaType("A", "9"));
            Assert.AreEqual(broadWithAssociated, GetSingleWmdaHlaType("B", "21"));
            Assert.AreEqual(splitWithAssociated, GetSingleWmdaHlaType("B", "51"));
            Assert.AreEqual(notSplitWithAssociated, GetSingleWmdaHlaType("DR", "14"));
        }
    }
}
