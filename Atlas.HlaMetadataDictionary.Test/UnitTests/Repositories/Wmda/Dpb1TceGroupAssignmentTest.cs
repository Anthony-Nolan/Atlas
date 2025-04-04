﻿using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Repositories.Wmda
{
    internal class Dpb1TceGroupAssignmentTest : WmdaRepositoryTestBase<Dpb1TceGroupAssignment>
    {
        protected override IEnumerable<Dpb1TceGroupAssignment> SelectTestDataTypings(WmdaDataset dataset) => dataset.Dpb1TceGroupAssignments;
        protected override string[] ApplicableLoci => MolecularLoci;

        [TestCase("17:01:01:01", "1", "1",
            Description = "Same assignment in V1 & 2.")]
        [TestCase("06:01:01:01", "3", "2",
            Description = "Assignment changed between V1 & V2.")]
        [TestCase("25:01", "", "2",
            Description = "V2 Assignment based on functional distance scores.")]
        [TestCase("08:01", "3", "2",
            Description = "V1 & V2 Assignments based on functional distance scores; Assignment changed between V1 & V2.")]
        [TestCase("18:01", "", "3",
            Description = "No assignment in V1.")]
        [TestCase("192:01", "", "",
            Description = "No Assignments in V1 & V2.")]
        [TestCase("551:01N", "", "",
            Description = "Null expressing allele")]
        public void WmdaDataRepository_ExpressingDpb1Alleles_TceGroupsSuccessfullyCaptured(
            string alleleName, string versionOne, string versionTwo)
        {
            var expectedAlleleStatus = new Dpb1TceGroupAssignment(alleleName, versionOne, versionTwo);

            const string locus = "DPB1*";
            var actualAlleleStatus = GetSingleWmdaHlaTyping(locus, alleleName);

            Assert.AreEqual(expectedAlleleStatus, actualAlleleStatus);
        }
    }
}
