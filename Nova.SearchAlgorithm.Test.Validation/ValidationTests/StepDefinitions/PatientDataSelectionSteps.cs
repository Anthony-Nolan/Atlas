using System;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources.SpecificTestCases;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions
{
    [Binding]
    public class PatientDataSelectionSteps
    {
        [Given(@"a patient has a match")]
        public void GivenAPatientHasAMatch()
        {
            var patientDataSelector = ScenarioContext.Current.Get<IPatientDataSelector>();
            ScenarioContext.Current.Set(patientDataSelector);
        }
        
        [Given(@"a set of (.*) patients with matching donors")]
        public void GivenASetOfXPatientsWithMatchingDonors(string numberOfPatientsString)
        {
            var multiplePatientDataSelector = ScenarioContext.Current.Get<IMultiplePatientDataSelector>();
            var numberOfPatients = int.Parse(numberOfPatientsString);
            multiplePatientDataSelector.SetNumberOfPatients(numberOfPatients);
            ScenarioContext.Current.Set(multiplePatientDataSelector);
        }   
        
        [Given(@"a set of patients with matching donors")]
        public void GivenASetOfPatientsWithMatchingDonors()
        {
            var multiplePatientDataSelector = ScenarioContext.Current.Get<IMultiplePatientDataSelector>();
            ScenarioContext.Current.Set(multiplePatientDataSelector);
        }

        [Given(@"the patient is untyped at Locus (.*)")]
        public void GivenThePatientIsUntypedAt(string locus)
        {
            var patientDataSelector = ScenarioContext.Current.Get<IPatientDataSelector>();

            switch (locus)
            {
                case "C":
                    patientDataSelector.SetPatientUntypedAtLocus(Locus.C);
                    break;
                case "Dpb1":
                    patientDataSelector.SetPatientUntypedAtLocus(Locus.Dpb1);
                    break;
                case "Dqb1":
                    patientDataSelector.SetPatientUntypedAtLocus(Locus.Dqb1);
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

        [Given(@"the patient is homozygous at (.*)")]
        public void GivenThePatientIsHomozygousAt(string locus)
        {
            var patientDataSelector = ScenarioContext.Current.Get<IPatientDataSelector>();

            switch (locus)
            {
                case "locus A":
                    patientDataSelector.SetPatientHomozygousAtLocus(Locus.A);
                    break;
                case "locus B":
                    patientDataSelector.SetPatientHomozygousAtLocus(Locus.B);
                    break;
                case "locus C":
                    patientDataSelector.SetPatientHomozygousAtLocus(Locus.C);
                    break;
                case "locus DPB1":
                    patientDataSelector.SetPatientHomozygousAtLocus(Locus.Dpb1);
                    break;
                case "locus DQB1":
                    patientDataSelector.SetPatientHomozygousAtLocus(Locus.Dqb1);
                    break;
                case "locus DRB1":
                    patientDataSelector.SetPatientHomozygousAtLocus(Locus.Drb1);
                    break;
                case "all loci":
                    patientDataSelector.SetPatientHomozygousAtLocus(Locus.A);
                    patientDataSelector.SetPatientHomozygousAtLocus(Locus.B);
                    patientDataSelector.SetPatientHomozygousAtLocus(Locus.C);
                    patientDataSelector.SetPatientHomozygousAtLocus(Locus.Dpb1);
                    patientDataSelector.SetPatientHomozygousAtLocus(Locus.Dqb1);
                    patientDataSelector.SetPatientHomozygousAtLocus(Locus.Drb1);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"the matching donor is a (.*) match")]
        public void GivenTheMatchingDonorIsOfMatchType(string matchType)
        {
            var patientDataSelector = ScenarioContext.Current.Get<IPatientDataSelector>();
            patientDataSelector.SetMatchType(matchType);
            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"each matching donor is a (.*) match")]
        public void GivenEachMatchingDonorIsOfMatchType(string matchType)
        {
            var selector = ScenarioContext.Current.Get<IMultiplePatientDataSelector>();
            selector.PatientDataSelectors = selector.PatientDataSelectors.Select(s => s.SetMatchType(matchType)).ToList();
            ScenarioContext.Current.Set(selector);
        }

        [Given(@"the matching donor is untyped at Locus (.*)")]
        public void GivenTheMatchingDonorIsUntypedAt(string locus)
        {
            var patientDataSelector = ScenarioContext.Current.Get<IPatientDataSelector>();

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
        public void GivenTheMatchingDonorIsOfDonorType(string donorType)
        {
            var patientDataSelector = ScenarioContext.Current.Get<IPatientDataSelector>();
            patientDataSelector.SetMatchDonorType(donorType);
            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"each matching donor is of type (.*)")]
        public void GivenEachMatchingDonorIsOfDonorType(string donorType)
        {
            var selector = ScenarioContext.Current.Get<IMultiplePatientDataSelector>();
            selector.PatientDataSelectors = selector.PatientDataSelectors.Select(s => s.SetMatchDonorType(donorType)).ToList();
            ScenarioContext.Current.Set(selector);
        }

        [Given(@"the matching donor is (.*) typed at (.*)")]
        public void GivenTheMatchingDonorIsHlaTyped(string typingCategory, string locus)
        {
            var patientDataSelector = ScenarioContext.Current.Get<IPatientDataSelector>();
            patientDataSelector.SetMatchTypingCategories(typingCategory, locus);
            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"each matching donor is (.*) typed at (.*)")]
        public void GivenEachMatchingDonorIsHlaTyped(string typingCategory, string locus)
        {
            var selector = ScenarioContext.Current.Get<IMultiplePatientDataSelector>();
            selector.PatientDataSelectors = selector.PatientDataSelectors.Select(s => s.SetMatchTypingCategories(typingCategory, locus)).ToList();
            ScenarioContext.Current.Set(selector);
        }

        [Given(@"the matching donor is homozygous at (.*)")]
        public void GivenTheMatchingDonorIsHomozygousAt(string locus)
        {
            var patientDataSelector = ScenarioContext.Current.Get<IPatientDataSelector>();

            switch (locus)
            {
                case "locus A":
                    patientDataSelector.SetMatchingDonorHomozygousAtLocus(Locus.A);
                    break;
                case "locus B":
                    patientDataSelector.SetMatchingDonorHomozygousAtLocus(Locus.B);
                    break;
                case "locus C":
                    patientDataSelector.SetMatchingDonorHomozygousAtLocus(Locus.C);
                    break;
                case "locus DPB1":
                    patientDataSelector.SetMatchingDonorHomozygousAtLocus(Locus.Dpb1);
                    break;
                case "locus DQB1":
                    patientDataSelector.SetMatchingDonorHomozygousAtLocus(Locus.Dqb1);
                    break;
                case "locus DRB1":
                    patientDataSelector.SetMatchingDonorHomozygousAtLocus(Locus.Drb1);
                    break;
                case "all loci":
                    patientDataSelector.SetMatchingDonorHomozygousAtLocus(Locus.A);
                    patientDataSelector.SetMatchingDonorHomozygousAtLocus(Locus.B);
                    patientDataSelector.SetMatchingDonorHomozygousAtLocus(Locus.C);
                    patientDataSelector.SetMatchingDonorHomozygousAtLocus(Locus.Dpb1);
                    patientDataSelector.SetMatchingDonorHomozygousAtLocus(Locus.Dqb1);
                    patientDataSelector.SetMatchingDonorHomozygousAtLocus(Locus.Drb1);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"the matching donor is in registry: (.*)")]
        public void GivenTheMatchingDonorIsInRegistry(string registry)
        {
            var patientDataSelector = ScenarioContext.Current.Get<IPatientDataSelector>();
            patientDataSelector.SetMatchDonorRegistry(registry);
            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"each matching donor is in registry: (.*)")]
        public void GivenEachMatchingDonorIsInRegistry(string registry)
        {
            var selector = ScenarioContext.Current.Get<IMultiplePatientDataSelector>();
            selector.PatientDataSelectors = selector.PatientDataSelectors.Select(s => s.SetMatchDonorRegistry(registry)).ToList();
            ScenarioContext.Current.Set(selector);
        }

        [Given(@"the match level is (.*)")]
        public void GivenTheMatchingDonorIsALevelMatch(string matchLevel)
        {
            var patientDataSelector = ScenarioContext.Current.Get<IPatientDataSelector>();

            switch (matchLevel)
            {
                case "p-group":
                    patientDataSelector.SetAsMatchLevelAtAllLoci(MatchLevel.PGroup);
                    break;
                case "g-group":
                    patientDataSelector.SetAsMatchLevelAtAllLoci(MatchLevel.GGroup);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            ScenarioContext.Current.Set(patientDataSelector);
        }
    }
}