using System;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions
{
    [Binding]
    public class PatientDataSelectionSteps
    {
        [Given(@"a patient has a match")]
        public void GivenAPatientHasAMatch()
        {
            var patientDataSelector = new PatientDataSelector {HasMatch = true};
            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"the matching donor is a (.*) match")]
        public void GivenAPatientIsAMatchOfType(string matchType)
        {
            var patientDataSelector = ScenarioContext.Current.Get<PatientDataSelector>();

            if (matchType == "10/10")
            {
                patientDataSelector.SetAsTenOutOfTenMatch();
            }
            else
            {
                ScenarioContext.Current.Pending();
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
                    patientDataSelector.MatchingTypingCategory = HlaTypingCategory.Tgs;
                    break;
                case "three field allele":
                    patientDataSelector.MatchingTypingCategory = HlaTypingCategory.ThreeField;
                    break;
                case "two field allele":
                    patientDataSelector.MatchingTypingCategory = HlaTypingCategory.TwoField;
                    break;
                case "XX code":
                    patientDataSelector.MatchingTypingCategory = HlaTypingCategory.XxCode;
                    break;
                case "NMDP code":
                    patientDataSelector.MatchingTypingCategory = HlaTypingCategory.NmdpCode;
                    break;
                case "serology":
                    patientDataSelector.MatchingTypingCategory = HlaTypingCategory.Serology;
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
    }
}