using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.PatientFactories;
using TechTalk.SpecFlow;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationTests.StepDefinitions.PatientDataSelection
{
    /// <summary>
    /// Contains step definitions for selecting patient data when a single patient corresponds to a multiple (database level) donors
    /// </summary>
    [Binding]
    public class MultipleDonorPatientDataSelectionSteps
    {
        private readonly ScenarioContext scenarioContext;

        public MultipleDonorPatientDataSelectionSteps(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }
        
        [Given(@"a patient has multiple matches at different typing resolutions")]
        public void GivenAPatientHasMultipleMatchesAtDifferentTypingResolutions()
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();

            var allResolutions = new[]
            {
                HlaTypingResolution.Tgs,
                HlaTypingResolution.ThreeFieldTruncatedAllele,
                HlaTypingResolution.TwoFieldTruncatedAllele,
                HlaTypingResolution.NmdpCode,
                HlaTypingResolution.XxCode,
                HlaTypingResolution.Serology,
                HlaTypingResolution.PGroup,
                HlaTypingResolution.GGroup,
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

            scenarioContext.Set(patientDataFactory);
        }

        [Given(@"a patient has multiple matches with different match counts")]
        public void GivenAPatientHasMultipleMatchesWithDifferentMatchCounts()
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();

            var expectedDatabaseDonors = new List<DatabaseDonorSpecification>
            {
                // 1 mismatch at A
                new DatabaseDonorSpecification
                {
                    ShouldMatchGenotype = new PhenotypeInfo<bool>().Map((l, p, noop) => !(l == Locus.A && p == LocusPosition.One)),
                },
                // 2 mismatches at A
                new DatabaseDonorSpecification
                {
                    ShouldMatchGenotype = new PhenotypeInfo<bool>().Map((l, p, noop) => l != Locus.A),
                },
                // 2 mismatches at A, 1 at B 
                new DatabaseDonorSpecification
                {
                    ShouldMatchGenotype = new PhenotypeInfo<bool>().Map((l, p, noop) => l != Locus.A && !(l == Locus.B && p == LocusPosition.One)),
                },
                // 2 mismatches at A, 2 at B
                new DatabaseDonorSpecification
                {
                    ShouldMatchGenotype = new PhenotypeInfo<bool>().Map((l, p, noop) => l != Locus.A && l != Locus.B),
                },
                // 1 mismatch at DQB1
                new DatabaseDonorSpecification
                {
                    ShouldMatchGenotype = new PhenotypeInfo<bool>().Map((l, p, noop) => l != Locus.Dqb1),
                },
                // No mismatches
                new DatabaseDonorSpecification
                {
                    ShouldMatchGenotype = new PhenotypeInfo<bool>(true),
                }
            };

            foreach (var databaseDonor in expectedDatabaseDonors)
            {
                patientDataFactory.AddExpectedDatabaseDonor(databaseDonor);
            }

            scenarioContext.Set(patientDataFactory);
        }

        [Given(@"a patient has multiple matches with different match confidences")]
        public void GivenAPatientHasMultipleMatchesWithDifferentMatchConfidences()
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();

            var resolutions = new[]
            {
                HlaTypingResolution.Unambiguous,
                HlaTypingResolution.AlleleStringOfNamesWithMultiplePGroups,
                HlaTypingResolution.AlleleStringOfNamesWithSinglePGroup
            };

            foreach (var resolution in resolutions)
            {
                patientDataFactory.AddFullDonorTypingResolution(new PhenotypeInfo<HlaTypingResolution>(resolution));
            }

            scenarioContext.Set(patientDataFactory);
        }

        [Given(@"all matching donors are of type (.*)")]
        public void GivenAllMatchingDonorsAreOfDonorType(string donorType)
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();
            patientDataFactory.SetMatchDonorType(donorType);
            scenarioContext.Set(patientDataFactory);
        }
    }
}