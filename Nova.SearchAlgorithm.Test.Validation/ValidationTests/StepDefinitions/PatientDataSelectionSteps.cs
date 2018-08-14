using System;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions
{
    [Binding]
    public class PatientDataSelectionSteps
    {
        [Given(@"a patient has a match")]
        public void GivenAPatientHasAMatch()
        {
            var metaDonorRepository = ScenarioContext.Current.Get<IMetaDonorRepository>();
            var patientDataSelector = new PatientDataSelector(metaDonorRepository) {HasMatch = true};
            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"the matching donor is a (.*) match")]
        public void GivenAPatientIsAMatchOfType(string matchType)
        {
            var patientDataSelector = ScenarioContext.Current.Get<PatientDataSelector>();

            switch (matchType)
            {
                case "10/10":
                    patientDataSelector.SetAsTenOutOfTenMatch();
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"the matching donor is untyped at Locus (.*)")]
        public void GivenTheMatchingDonorIsUntypedAt(string locus)
        {
            var patientDataSelector = ScenarioContext.Current.Get<PatientDataSelector>();

            switch (locus)
            {
                case "C":
                    patientDataSelector.SetMatchingDonorUntypedAtLocus(Locus.C);
                    break;
                case "Dpb1":
                    patientDataSelector.SetMatchingDonorUntypedAtLocus(Locus.Dpb1);
                    break;
                case "Dqb1":
                    patientDataSelector.SetMatchingDonorUntypedAtLocus(Locus.Dqb1);
                    break;
                case "A":
                case "B":
                case "Drb1":
                    throw new Exception("Loci A, B, DRB1 cannot be untyped");
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"the matching donor is of type (.*)")]
        public void GivenTheMatchingDonorIsOfDonorType(string donorTypeString)
        {
            var donorType = (DonorType) Enum.Parse(typeof(DonorType), donorTypeString, true);
            var patientDataSelector = ScenarioContext.Current.Get<PatientDataSelector>();

            patientDataSelector.MatchingDonorTypes.Add(donorType);

            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"the matching donor is (.*) typed")]
        public void GivenTheMatchingDonorIsHlaTyped(string typingCategory)
        {
            var patientDataSelector = ScenarioContext.Current.Get<PatientDataSelector>();

            switch (typingCategory)
            {
                case "TGS":
                    patientDataSelector.SetFullMatchingTypingCategory(HlaTypingCategory.TgsFourFieldAllele);
                    break;
                case "three field truncated allele":
                    patientDataSelector.SetFullMatchingTypingCategory(HlaTypingCategory.ThreeFieldTruncatedAllele);
                    break;
                case "two field truncated allele":
                    patientDataSelector.SetFullMatchingTypingCategory(HlaTypingCategory.TwoFieldTruncatedAllele);
                    break;
                case "XX code":
                    patientDataSelector.SetFullMatchingTypingCategory(HlaTypingCategory.XxCode);
                    break;
                case "NMDP code":
                    patientDataSelector.SetFullMatchingTypingCategory(HlaTypingCategory.NmdpCode);
                    break;
                case "serology":
                    patientDataSelector.SetFullMatchingTypingCategory(HlaTypingCategory.Serology);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"the matching donor is in registry: (.*)")]
        public void GivenTheMatchingDonorIsInRegistry(string registryString)
        {
            var patientDataSelector = ScenarioContext.Current.Get<PatientDataSelector>();

            switch (registryString)
            {
                case "Anthony Nolan":
                    patientDataSelector.MatchingRegistries.Add(RegistryCode.AN);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            ScenarioContext.Current.Set(patientDataSelector);
        }
        
        [Given(@"the match level is (.*)")]
        public void GivenTheMatchingDonorIsALevelMatch(string matchLevel)
        {
            var patientDataSelector = ScenarioContext.Current.Get<PatientDataSelector>();

            switch (matchLevel)
            {
                case "p-group":
                    patientDataSelector.SetAsMatchLevelAtAllLoci(MatchLevel.PGroup);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            ScenarioContext.Current.Set(patientDataSelector);
        }
    }
}