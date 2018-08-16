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
            var alleleRepository = ScenarioContext.Current.Get<IAlleleRepository>();
            var patientDataSelector = new PatientDataSelector(metaDonorRepository, alleleRepository) {HasMatch = true};
            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"the patient is untyped at Locus (.*)")]
        public void GivenThePatientIsUntypedAt(string locus)
        {
            var patientDataSelector = ScenarioContext.Current.Get<PatientDataSelector>();

            switch (locus)
            {
                case "C":
                    patientDataSelector.SetPatientUntypedAt(Locus.C);
                    break;
                case "Dpb1":
                    patientDataSelector.SetPatientUntypedAt(Locus.Dpb1);
                    break;
                case "Dqb1":
                    patientDataSelector.SetPatientUntypedAt(Locus.Dqb1);
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

        [Given(@"the matching donor is a (.*) match")]
        public void GivenTheMatchingDonorIsOfMatchType(string matchType)
        {
            var patientDataSelector = ScenarioContext.Current.Get<PatientDataSelector>();

            switch (matchType)
            {
                case "10/10":
                    patientDataSelector.SetAsTenOutOfTenMatch();
                    break;
                case "8/8":
                    patientDataSelector.SetAsEightOutOfEightMatch();
                    break;
                case "6/6":
                    patientDataSelector.SetAsSixOutOfSixMatch();
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
                case "TGS (four field)":
                    patientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.Tgs);
                    patientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    break;
                case "TGS (three field)":
                    patientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.Tgs);
                    patientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.ThreeFieldAllele);
                    break;
                case "TGS (two field)":
                    patientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.Tgs);
                    patientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.TwoFieldAllele);
                    break;
                case "three field truncated allele":
                    patientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.ThreeFieldTruncatedAllele);
                    break;
                case "two field truncated allele":
                    patientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.TwoFieldTruncatedAllele);
                    break;
                case "XX code":
                    patientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.XxCode);
                    break;
                case "NMDP code":
                    patientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.NmdpCode);
                    break;
                case "serology":
                    patientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.Serology);
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