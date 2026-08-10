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
        [TestCase("B", "51", new string[] { }, new[] { "5101", "5102", "5103", "5107", "5119" }, Description = "Split serology with associated")]
        [TestCase("DR", "14", new string[] { }, new[] { "1401", "1402", "1403", "1404", "1405", "1411", "1414", "1422", "1424", "1448" }, Description = "Not-split serology with associated")]
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