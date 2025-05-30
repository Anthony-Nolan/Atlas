using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Repositories.Wmda
{
    internal class SerologyToSerologyRelationshipsTest : WmdaRepositoryTestBase<RelSerSer>
    {
        protected override IEnumerable<RelSerSer> SelectTestDataTypings(WmdaDataset dataset) => dataset.SerologyToSerologyRelationships;
        protected override string[] ApplicableLoci => SerologyLoci;

        [TestCase("A", "9", new[] { "23", "24" }, new string[] { }, Description = "Broad serology with splits, but no associated")]
        [TestCase("B", "21", new[] { "49", "50" }, new[] { "4005" }, Description = "Broad serology with splits & associated")]
        [TestCase("B", "51", new string[] { }, new[] { "5102", "5103" }, Description = "Split serology with associated")]
        [TestCase("DR", "14", new string[] { }, new[] { "1403", "1404" }, Description = "Not-split serology with associated")]
        public void WmdaDataRepository_WhenSerologyHasRelatedSerology_RelationshipsSuccessfullyCaptured(
            string locus,
            string serologyName,
            string[] expectedSplits,
            string[] expectedAssociated)
        {
            var expectedRelationship = new RelSerSer(locus, serologyName, expectedSplits, expectedAssociated);

            var actualRelationship = GetSingleWmdaHlaTyping(locus, serologyName);

            Assert.AreEqual(expectedRelationship, actualRelationship);
        }
    }
}