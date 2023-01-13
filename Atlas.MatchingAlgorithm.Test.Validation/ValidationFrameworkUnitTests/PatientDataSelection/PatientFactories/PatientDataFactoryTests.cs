using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.DataSelectors;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.PatientFactories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationFrameworkUnitTests.PatientDataSelection.PatientFactories
{
    [TestFixture]
    public class PatientDataFactoryTests
    {
        private PatientDataFactory patientDataFactory;
        private IMetaDonorSelector metaDonorSelector;
        private IDatabaseDonorSelector databaseDonorSelector;
        private IPatientHlaSelector patientHlaSelector;

        private MetaDonorSelectionCriteria capturedMetaDonorCriteria;
        private List<DatabaseDonorSpecification> capturedDatabaseDonorCriteria;
        private PatientHlaSelectionCriteria capturedPatientCriteria;

        [SetUp]
        public void SetUp()
        {
            metaDonorSelector = Substitute.For<IMetaDonorSelector>();
            databaseDonorSelector = Substitute.For<IDatabaseDonorSelector>();
            patientHlaSelector = Substitute.For<IPatientHlaSelector>();

            capturedDatabaseDonorCriteria = new List<DatabaseDonorSpecification>();

            metaDonorSelector.GetMetaDonor(Arg.Do<MetaDonorSelectionCriteria>(x => capturedMetaDonorCriteria = x));
            databaseDonorSelector.GetExpectedMatchingDonorId(
                Arg.Any<MetaDonor>(),
                Arg.Do<DatabaseDonorSpecification>(x => capturedDatabaseDonorCriteria.Add(x))
            );
            patientHlaSelector.GetPatientHla(Arg.Any<MetaDonor>(), Arg.Do<PatientHlaSelectionCriteria>(x => capturedPatientCriteria = x));

            patientDataFactory = new PatientDataFactory(metaDonorSelector, databaseDonorSelector, patientHlaSelector);
        }

        [Test]
        public void SetAsMatchLevelAtAllLoci_SetsMatchLevelInMetaDonorAndPatientCriteria()
        {
            const MatchLevel matchLevel = MatchLevel.GGroup;

            patientDataFactory.SetAsMatchLevelAtAllLoci(matchLevel);

            CaptureCriteria();
            // Ensure that the match levels have actually updated, as the default values will be the same
            capturedMetaDonorCriteria.MatchLevels.A.Position1.Should().Be(matchLevel);
            capturedMetaDonorCriteria.MatchLevels.Should().BeEquivalentTo(capturedPatientCriteria.MatchLevels);
        }

        [Test]
        public void SetPatientHomozygousAtLocus_WhenShouldBeDoubleMatch_SetsDonorAsHomozygous()
        {
            const Locus locus = Locus.A;

            patientDataFactory.SetPatientHomozygousAtLocus(locus);

            patientDataFactory.GetPatientHla();
            capturedMetaDonorCriteria.IsHomozygous.GetLocus(locus).Should().BeTrue();
        }

        [Test]
        public void SetPatientHomozygousAtLocus_WhenMismatchAllowed_DoesNotSetDonorAsHomozygous()
        {
            const Locus locus = Locus.A;
            patientDataFactory.SetMismatchAtPosition(locus, LocusPosition.One);
            patientDataFactory.SetPatientHomozygousAtLocus(locus);

            CaptureCriteria();
            capturedMetaDonorCriteria.IsHomozygous.GetLocus(locus).Should().BeFalse();
        }

        [Test]
        public void SetPatientHomozygousAtLocus_SetsPatientTypingResolutionAsTgs()
        {
            const Locus locus = Locus.A;
            patientDataFactory.SetPatientHomozygousAtLocus(locus);

            CaptureCriteria();
            var expectedResolutions = new LocusInfo<HlaTypingResolution>(HlaTypingResolution.Tgs);
            capturedPatientCriteria.PatientTypingResolutions.GetLocus(locus).Should().BeEquivalentTo(expectedResolutions);
        }

        [Test]
        public void AddFullDonorTypingResolution_AddsDatabaseDonorToMetaDonorAndDatabaseDonorCriteria()
        {
            patientDataFactory.AddFullDonorTypingResolution(new PhenotypeInfo<HlaTypingResolution>(HlaTypingResolution.NmdpCode));

            CaptureCriteria();
            // Ensure donor is added, as default values are equivalent
            capturedMetaDonorCriteria.DatabaseDonorDetailsSets.Count.Should().BeGreaterThan(1);
            capturedMetaDonorCriteria.DatabaseDonorDetailsSets.Should().BeEquivalentTo(capturedDatabaseDonorCriteria);
        }
        
        [Test]
        public void AddExpectedDatabaseDonor_AddsDatabaseDonorToMetaDonorAndDatabaseDonorCriteria()
        {
            patientDataFactory.AddExpectedDatabaseDonor(new DatabaseDonorSpecification());

            CaptureCriteria();
            // Ensure donor is added, as default values are equivalent
            capturedMetaDonorCriteria.DatabaseDonorDetailsSets.Count.Should().BeGreaterThan(1);
            capturedMetaDonorCriteria.DatabaseDonorDetailsSets.Should().BeEquivalentTo(capturedDatabaseDonorCriteria);
        }
        
        [Test]
        public void UpdateMatchingDonorTypingResolutionsAtLocus_UpdatesMetaDonorAndDatabaseDonorCriteria()
        {
            const HlaTypingResolution resolution = HlaTypingResolution.Serology;
            const Locus locus = Locus.A;
            
            patientDataFactory.UpdateMatchingDonorTypingResolutionsAtLocus(locus, resolution);

            CaptureCriteria();
            capturedMetaDonorCriteria.DatabaseDonorDetailsSets.First().MatchingTypingResolutions.GetLocus(locus).Position1.Should().Be(resolution);
            capturedDatabaseDonorCriteria.First().MatchingTypingResolutions.GetLocus(locus).Position1.Should().Be(resolution);
        }
        
        [Test]
        public void UpdateMatchingDonorTypingResolutionsAtLocus_UpdatesAllDatabaseDonorCriteria()
        {
            const HlaTypingResolution resolution = HlaTypingResolution.Serology;
            const Locus locus = Locus.A;
            patientDataFactory.AddExpectedDatabaseDonor(new DatabaseDonorSpecification());
            
            patientDataFactory.UpdateMatchingDonorTypingResolutionsAtLocus(locus, resolution);

            CaptureCriteria();
            capturedDatabaseDonorCriteria.First().MatchingTypingResolutions.GetLocus(locus).Position1.Should().Be(resolution);
            capturedDatabaseDonorCriteria.Skip(1).First().MatchingTypingResolutions.GetLocus(locus).Position1.Should().Be(resolution);
        }
        
        [Test]
        public void UpdateMatchingDonorTypingResolutionsAtAllLoci_UpdatesAllLoci()
        {
            const HlaTypingResolution resolution = HlaTypingResolution.ThreeFieldTruncatedAllele;
            
            patientDataFactory.UpdateMatchingDonorTypingResolutionsAtAllLoci(resolution);

            CaptureCriteria();
            var resolutions = capturedDatabaseDonorCriteria.First().MatchingTypingResolutions;
            resolutions.GetLocus(Locus.A).Position1.Should().Be(resolution);
            resolutions.GetLocus(Locus.B).Position1.Should().Be(resolution);
            resolutions.GetLocus(Locus.C).Position1.Should().Be(resolution);
            resolutions.GetLocus(Locus.Dpb1).Position1.Should().Be(resolution);
            resolutions.GetLocus(Locus.Dqb1).Position1.Should().Be(resolution);
            resolutions.GetLocus(Locus.Drb1).Position1.Should().Be(resolution);
        }

        [Test]
        public void UpdateDonorGenotypeMatchGetPosition_UpdatesMetaDonorAndDatabaseDonorCriteria()
        {
            const Locus locus = Locus.A;
            const LocusPosition position = LocusPosition.One;
            const bool shouldMatchGenotype = false;
            
            patientDataFactory.UpdateDonorGenotypeMatchGetPosition(locus, position, shouldMatchGenotype);
            
            CaptureCriteria();
            capturedMetaDonorCriteria.DatabaseDonorDetailsSets.First().ShouldMatchGenotype.GetPosition(locus, position).Should().Be(shouldMatchGenotype);
            capturedDatabaseDonorCriteria.First().ShouldMatchGenotype.GetPosition(locus, position).Should().Be(shouldMatchGenotype);
        }
        
        [Test]
        public void UpdateDonorGenotypeMatchGetPosition_UpdatesAllDatabaseDonorCriteria()
        {
            const Locus locus = Locus.A;
            const LocusPosition position = LocusPosition.One;
            const bool shouldMatchGenotype = false;
            patientDataFactory.AddExpectedDatabaseDonor(new DatabaseDonorSpecification());
            
            patientDataFactory.UpdateDonorGenotypeMatchGetPosition(locus, position, shouldMatchGenotype);

            CaptureCriteria();
            capturedDatabaseDonorCriteria.First().ShouldMatchGenotype.GetPosition(locus, position).Should().Be(shouldMatchGenotype);
            capturedDatabaseDonorCriteria.Skip(1).First().ShouldMatchGenotype.GetPosition(locus, position).Should().Be(shouldMatchGenotype);
        }

        /// <summary>
        /// Fetches patient hla and database donors.
        /// Doing so allows us to capture the criteria that have been built up by the patient selector, and make assertions on the final criteria
        /// The criteria capture itseld is set up in the SetUp method when we create mocks - this method ensures that the capturing has occurred
        /// </summary>
        private void CaptureCriteria()
        {
            patientDataFactory.GetPatientHla();

            // Must call ToList() to evaluate enumerable, and capture criteria
            patientDataFactory.GetExpectedMatchingDonorIds().ToList();
        }
    }
}