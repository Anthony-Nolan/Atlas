using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.PatientFactories;
using System.Linq;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions.PatientDataSelection
{
    /// <summary>
    /// Contains step definitions for selecting patient data when a single patient corresponds to a multiple (database level) donors
    /// </summary>
    [Binding]
    public class MultipleDonorPatientDataSelectionSteps
    {
        [Given(@"a patient has multiple matches at different typing resolutions")]
        public void GivenAPatientHasMultipleMatchesAtDifferentTypingResolutions()
        {
            var patientDataFactory = ScenarioContext.Current.Get<IPatientDataFactory>();

            var allResolutions = new[]
            {
                HlaTypingResolution.Tgs,
                HlaTypingResolution.ThreeFieldTruncatedAllele,
                HlaTypingResolution.TwoFieldTruncatedAllele,
                HlaTypingResolution.NmdpCode,
                HlaTypingResolution.XxCode,
                HlaTypingResolution.Serology,
                HlaTypingResolution.Arbitrary,
                HlaTypingResolution.AlleleStringOfNames,
            };
            var resolutionSets = allResolutions.Select(r => new PhenotypeInfo<HlaTypingResolution>(r));
            // Resolutions include 2/3 field truncated, so genotype must be four-field TGS typed
            patientDataFactory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
            foreach (var resolutionSet in resolutionSets)
            {
                patientDataFactory.AddFullDonorTypingResolution(resolutionSet);
            }

            ScenarioContext.Current.Set(patientDataFactory);
        }
        
        [Given(@"a patient has multiple matches with different match counts")]
        public void GivenAPatientHasMultipleMatchesWithDifferentMatchCounts()
        {
            var patientDataFactory = ScenarioContext.Current.Get<IPatientDataFactory>();

            var expectedDatabaseDonors = new List<DatabaseDonorSpecification>
            {
                // 1 mismatch at A
                new DatabaseDonorSpecification
                {
                    ShouldMatchGenotype = new PhenotypeInfo<bool>().Map((l, p, noop) => !(l == Locus.A && p == TypePositions.One)),
                },
                // 2 mismatches at A
                new DatabaseDonorSpecification
                {
                    ShouldMatchGenotype = new PhenotypeInfo<bool>().Map((l, p, noop) => l != Locus.A),
                },
                // 2 mismatches at A, 1 at B 
                new DatabaseDonorSpecification
                {
                    ShouldMatchGenotype = new PhenotypeInfo<bool>().Map((l, p, noop) => l != Locus.A && !(l == Locus.B && p == TypePositions.One)),
                },
                // 2 mismatches at A, 2 at B
                new DatabaseDonorSpecification
                {
                    ShouldMatchGenotype = new PhenotypeInfo<bool>().Map((l, p, noop) => l != Locus.A && l != Locus.B),
                }
            };
            
            foreach (var databaseDonor in expectedDatabaseDonors)
            {
                patientDataFactory.AddExpectedDatabaseDonor(databaseDonor);
            }

            ScenarioContext.Current.Set(patientDataFactory);
        }
        
        [Given(@"all matching donors are of type (.*)")]
        public void GivenAllMatchingDonorsAreOfDonorType(string donorType)
        {
            var patientDataFactory = ScenarioContext.Current.Get<IPatientDataFactory>();
            patientDataFactory.SetMatchDonorType(donorType);
            ScenarioContext.Current.Set(patientDataFactory);
        }
        
        [Given(@"all matching donors are in registry: (.*)")]
        public void GivenAllMatchingDonorsAreInRegistry(string registry)
        {
            var patientDataFactory = ScenarioContext.Current.Get<IPatientDataFactory>();
            patientDataFactory.SetMatchDonorRegistry(registry);
            ScenarioContext.Current.Set(patientDataFactory);
        }
    }
}