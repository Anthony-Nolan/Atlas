using System.Collections.Generic;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationFrameworkUnitTests
{
    [TestFixture]
    public class AlleleStringAlleleSelectorTests
    {
        [Test]
        public void GetAllelesForAlleleStringOfNamesWithSinglePGroup_WhenNoAllelesProvided_ReturnsEmptyList()
        {
            var alleles = AlleleStringAlleleSelector.GetAllelesForAlleleStringOfNamesWithSinglePGroup(
                new AlleleTestData{ AlleleName = "name", PGroup = "p-group"},
                new List<AlleleTestData>()
            );

            alleles.Should().BeEmpty();
        }
        
        [Test]
        public void GetAllelesForAlleleStringOfNamesWithSinglePGroup_WhenPGroupOfSelectedAlleleUnknown_ReturnsEmptyList()
        {
            var alleles = AlleleStringAlleleSelector.GetAllelesForAlleleStringOfNamesWithSinglePGroup(
                new AlleleTestData{ AlleleName = "name", PGroup = null},
                new List<AlleleTestData>{ new AlleleTestData{ AlleleName = "otherName", PGroup = "p-group"}}
            );

            alleles.Should().BeEmpty();
        }
        
        [Test]
        public void GetAllelesForAlleleStringOfNamesWithSinglePGroup_WhenNoAllelesShareAPGroupWithSelectedAllele_ReturnsEmptyList()
        {
            var alleles = AlleleStringAlleleSelector.GetAllelesForAlleleStringOfNamesWithSinglePGroup(
                new AlleleTestData{ AlleleName = "name", PGroup = "p-group-other"},
                new List<AlleleTestData>{ new AlleleTestData{ AlleleName = "otherName", PGroup = "p-group"}}
            );

            alleles.Should().BeEmpty();
        }
        
        [Test]
        public void GetAllelesForAlleleStringOfNamesWithSinglePGroup_WhenAllelesShareAPGroupWithSelectedAllele_ReturnsNonEmptyList()
        {
            var alleles = AlleleStringAlleleSelector.GetAllelesForAlleleStringOfNamesWithSinglePGroup(
                new AlleleTestData{ AlleleName = "name", PGroup = "p-group"},
                new List<AlleleTestData>{ new AlleleTestData{ AlleleName = "otherName", PGroup = "p-group"}}
            );

            alleles.Should().NotBeEmpty();
        }
    }
}