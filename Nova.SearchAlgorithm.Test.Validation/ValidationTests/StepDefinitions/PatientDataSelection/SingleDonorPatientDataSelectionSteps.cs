using System;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions
{
    /// <summary>
    /// Contains step definitions for selecting patient data when a single patient corresponds to a single donor
    /// </summary>
    [Binding]
    public class SingleDonorPatientDataSelectionSteps
    {
        [Given(@"a patient and a donor")]
        public void GivenAPatientAndADonor()
        {
            var patientDataSelector = ScenarioContext.Current.Get<ISingleDonorPatientDataSelector>();
            ScenarioContext.Current.Set((IPatientHlaContainer) patientDataSelector);
        }

        [Given(@"a patient has a match")]
        public void GivenAPatientHasAMatch()
        {
            var patientDataSelector = ScenarioContext.Current.Get<ISingleDonorPatientDataSelector>();
            ScenarioContext.Current.Set((IPatientHlaContainer) patientDataSelector);
        }

        [Given(@"the patient is untyped at Locus (.*)")]
        public void GivenThePatientIsUntypedAt(string locus)
        {
            var patientDataSelector = ScenarioContext.Current.Get<ISingleDonorPatientDataSelector>();

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
            var patientDataSelector = ScenarioContext.Current.Get<ISingleDonorPatientDataSelector>();

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

        [Given(@"the donor is a (.*) match")]
        [Given(@"the matching donor is a (.*) match")]
        public void GivenTheMatchingDonorIsOfMatchType(string matchType)
        {
            var patientDataSelector = (SingleDonorPatientDataSelector) ScenarioContext.Current.Get<ISingleDonorPatientDataSelector>();
            patientDataSelector.SetMatchType(matchType);
            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"the donor has a (.*) mismatch at (.*)")]
        [Given(@"the matching donor has a (.*) mismatch at (.*)")]
        public void GivenTheMatchingDonorHasAMismatchAt(string mismatchType, string locus)
        {
            var patientDataSelector = ScenarioContext.Current.Get<ISingleDonorPatientDataSelector>();
            patientDataSelector.SetMismatches(mismatchType, locus);
            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"the donor is untyped at Locus (.*)")]
        [Given(@"the matching donor is untyped at Locus (.*)")]
        public void GivenTheMatchingDonorIsUntypedAt(string locus)
        {
            var patientDataSelector = ScenarioContext.Current.Get<ISingleDonorPatientDataSelector>();

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

        [Given(@"the donor is of type (.*)")]
        [Given(@"the matching donor is of type (.*)")]
        public void GivenTheMatchingDonorIsOfDonorType(string donorType)
        {
            var patientDataSelector = ScenarioContext.Current.Get<ISingleDonorPatientDataSelector>();
            patientDataSelector.SetMatchDonorType(donorType);
            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"the donor is (.*) typed at (.*)")]
        [Given(@"the matching donor is (.*) typed at (.*)")]
        public void GivenTheMatchingDonorIsHlaTyped(string typingCategory, string locus)
        {
            var patientDataSelector = ScenarioContext.Current.Get<ISingleDonorPatientDataSelector>();
            patientDataSelector.SetMatchTypingCategories(typingCategory, locus);
            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"the donor is homozygous at (.*)")]
        [Given(@"the matching donor is homozygous at (.*)")]
        public void GivenTheMatchingDonorIsHomozygousAt(string locus)
        {
            var patientDataSelector = ScenarioContext.Current.Get<ISingleDonorPatientDataSelector>();

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

        [Given(@"the donor is in registry: (.*)")]
        [Given(@"the matching donor is in registry: (.*)")]
        public void GivenTheMatchingDonorIsInRegistry(string registry)
        {
            var patientDataSelector = ScenarioContext.Current.Get<ISingleDonorPatientDataSelector>();
            patientDataSelector.SetMatchDonorRegistry(registry);
            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"the match level is (.*)")]
        public void GivenTheMatchingDonorIsALevelMatch(string matchLevel)
        {
            var patientDataSelector = ScenarioContext.Current.Get<ISingleDonorPatientDataSelector>();
            patientDataSelector.SetMatchLevelAtAllLoci(matchLevel);
            ScenarioContext.Current.Set(patientDataSelector);
        }
    }
}