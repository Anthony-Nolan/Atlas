using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using NUnit.Framework;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    [TestFixtureSource(typeof(WmdaRepositoryTestFixtureArgs), nameof(WmdaRepositoryTestFixtureArgs.RelSerSerTestArgs))]
    public class SerologyToSerologyRelationshipsTest : WmdaRepositoryTestBase<RelSerSer>
    {
        public SerologyToSerologyRelationshipsTest(IEnumerable<RelSerSer> relSerSer, IEnumerable<string> matchLoci)
            : base(relSerSer, matchLoci)
        {
        }

        [Test]
        public void SerologyToSerologyRelationships_SuccessfullyCaptured()
        {
            var broadNoAssociated = new RelSerSer("A", "9", new List<string> { "23", "24" }, new List<string>());
            var broadWithAssociated = new RelSerSer("B", "21", new List<string> { "49", "50" }, new List<string> { "4005" });
            var splitWithAssociated = new RelSerSer("B", "51", new List<string>(), new List<string> { "5102", "5103" });
            var notSplitWithAssociated = new RelSerSer("DR", "14", new List<string>(), new List<string> { "1403", "1404" });

            Assert.AreEqual(broadNoAssociated, GetSingleWmdaHlaTyping("A", "9"));
            Assert.AreEqual(broadWithAssociated, GetSingleWmdaHlaTyping("B", "21"));
            Assert.AreEqual(splitWithAssociated, GetSingleWmdaHlaTyping("B", "51"));
            Assert.AreEqual(notSplitWithAssociated, GetSingleWmdaHlaTyping("DR", "14"));
        }
    }
}
