using System;
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

        [Given(@"the matching donor is a (.*) match")]
        public void GivenTheMatchingDonorIsOfMatchType(string matchType)
        {
            var patientDataSelector = ScenarioContext.Current.Get<IPatientDataSelector>();

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
        public void GivenTheMatchingDonorIsOfDonorType(string donorTypeString)
        {
            var donorType = (DonorType) Enum.Parse(typeof(DonorType), donorTypeString, true);
            var patientDataSelector = ScenarioContext.Current.Get<IPatientDataSelector>();

            patientDataSelector.SetMatchingDonorType(donorType);

            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"the matching donor is (.*) typed at (.*)")]
        public void GivenTheMatchingDonorIsHlaTyped(string typingCategory, string locus)
        {
            var patientDataSelector = ScenarioContext.Current.Get<IPatientDataSelector>();

            if (locus == "each locus")
            {
                patientDataSelector = SetTypingCategoryAtAllLoci(patientDataSelector, typingCategory);
            }
            else
            {
                ScenarioContext.Current.Pending();
            }

            ScenarioContext.Current.Set(patientDataSelector);
        }

        [Given(@"the matching donor homozygous at (.*)")]
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

        private static IPatientDataSelector SetTypingCategoryAtAllLoci(IPatientDataSelector patientDataSelector, string typingCategory)
        {
            switch (typingCategory)
            {
                case "differently":
                    patientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    foreach (var resolution in TestCaseTypingResolutions.DifferentLociResolutions)
                    {
                        patientDataSelector.SetMatchingTypingResolutionAtLocus(resolution.Key, resolution.Value);
                    }
                    break;
                case "TGS":
                    patientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.Arbitrary);
                    patientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.Tgs);
                    break;
                case "TGS (four field)":
                    patientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    patientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.Tgs);
                    break;
                case "TGS (three field)":
                    patientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.ThreeFieldAllele);
                    patientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.Tgs);
                    break;
                case "TGS (two field)":
                    patientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.TwoFieldAllele);
                    patientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.Tgs);
                    break;
                case "three field truncated allele":
                    patientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    patientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.ThreeFieldTruncatedAllele);
                    break;
                case "two field truncated allele":
                    patientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    patientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.TwoFieldTruncatedAllele);
                    break;
                case "XX code":
                    patientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.Arbitrary);
                    patientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.XxCode);
                    break;
                case "NMDP code":
                    patientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.Arbitrary);
                    patientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.NmdpCode);
                    break;
                case "serology":
                    patientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.Arbitrary);
                    patientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.Serology);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return patientDataSelector;
        }

        [Given(@"the matching donor is in registry: (.*)")]
        public void GivenTheMatchingDonorIsInRegistry(string registryString)
        {
            var patientDataSelector = ScenarioContext.Current.Get<IPatientDataSelector>();

            switch (registryString)
            {
                case "Anthony Nolan":
                    patientDataSelector.SetMatchingRegistry(RegistryCode.AN);
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