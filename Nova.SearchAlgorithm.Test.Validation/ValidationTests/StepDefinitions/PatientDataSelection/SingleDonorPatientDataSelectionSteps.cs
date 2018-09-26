using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.PatientFactories;
using System;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions.PatientDataSelection
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
        }

        [Given(@"a patient has a match")]
        public void GivenAPatientHasAMatch()
        {
        }

        [Given(@"the patient is untyped at Locus (.*)")]
        public void GivenThePatientIsUntypedAt(string locus)
        {
            var patientDataFactory = ScenarioContext.Current.Get<IPatientDataFactory>();

            switch (locus)
            {
                case "C":
                    patientDataFactory.SetPatientUntypedAtLocus(Locus.C);
                    break;
                case "Dpb1":
                    patientDataFactory.SetPatientUntypedAtLocus(Locus.Dpb1);
                    break;
                case "Dqb1":
                    patientDataFactory.SetPatientUntypedAtLocus(Locus.Dqb1);
                    break;
                case "A":
                case "B":
                case "Drb1":
                    throw new Exception("Loci A, B, DRB1 cannot be untyped");
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            ScenarioContext.Current.Set(patientDataFactory);
        }
        
        [Given(@"the patient is (.*) typed at (.*)")]
        public void GivenThePatientIsUntypedAt(string typingCategory, string locus)
        {
            var patientDataFactory = ScenarioContext.Current.Get<IPatientDataFactory>();
            patientDataFactory.SetPatientTypingCategoryAt(typingCategory, locus);
            ScenarioContext.Current.Set(patientDataFactory);
        }

        [Given(@"the patient is homozygous at (.*)")]
        public void GivenThePatientIsHomozygousAt(string locus)
        {
            var patientDataFactory = ScenarioContext.Current.Get<IPatientDataFactory>();

            switch (locus)
            {
                case "locus A":
                    patientDataFactory.SetPatientHomozygousAtLocus(Locus.A);
                    break;
                case "locus B":
                    patientDataFactory.SetPatientHomozygousAtLocus(Locus.B);
                    break;
                case "locus C":
                    patientDataFactory.SetPatientHomozygousAtLocus(Locus.C);
                    break;
                case "locus DPB1":
                    patientDataFactory.SetPatientHomozygousAtLocus(Locus.Dpb1);
                    break;
                case "locus DQB1":
                    patientDataFactory.SetPatientHomozygousAtLocus(Locus.Dqb1);
                    break;
                case "locus DRB1":
                    patientDataFactory.SetPatientHomozygousAtLocus(Locus.Drb1);
                    break;
                case "all loci":
                    patientDataFactory.SetPatientHomozygousAtLocus(Locus.A);
                    patientDataFactory.SetPatientHomozygousAtLocus(Locus.B);
                    patientDataFactory.SetPatientHomozygousAtLocus(Locus.C);
                    patientDataFactory.SetPatientHomozygousAtLocus(Locus.Dpb1);
                    patientDataFactory.SetPatientHomozygousAtLocus(Locus.Dqb1);
                    patientDataFactory.SetPatientHomozygousAtLocus(Locus.Drb1);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            ScenarioContext.Current.Set(patientDataFactory);
        }

        [Given(@"the donor is a (.*) match")]
        [Given(@"the matching donor is a (.*) match")]
        public void GivenTheMatchingDonorIsOfMatchType(string matchType)
        {
            var patientDataFactory = (PatientDataFactory) ScenarioContext.Current.Get<IPatientDataFactory>();
            patientDataFactory.SetMatchType(matchType);
            ScenarioContext.Current.Set(patientDataFactory);
        }

        [Given(@"the donor has a (.*) mismatch at (.*)")]
        [Given(@"the matching donor has a (.*) mismatch at (.*)")]
        public void GivenTheMatchingDonorHasAMismatchAt(string mismatchType, string locus)
        {
            var patientDataFactory = ScenarioContext.Current.Get<IPatientDataFactory>();
            patientDataFactory.SetMismatches(mismatchType, locus);
            ScenarioContext.Current.Set(patientDataFactory);
        }

        [Given(@"the donor is untyped at Locus (.*)")]
        [Given(@"the matching donor is untyped at Locus (.*)")]
        public void GivenTheMatchingDonorIsUntypedAt(string locus)
        {
            var patientDataFactory = ScenarioContext.Current.Get<IPatientDataFactory>();

            switch (locus)
            {
                case "C":
                    patientDataFactory.UpdateMatchingDonorTypingResolutionsAtLocus(Locus.C, HlaTypingResolution.Untyped);
                    break;
                case "Dpb1":
                    patientDataFactory.UpdateMatchingDonorTypingResolutionsAtLocus(Locus.Dpb1, HlaTypingResolution.Untyped);
                    break;
                case "Dqb1":
                    patientDataFactory.UpdateMatchingDonorTypingResolutionsAtLocus(Locus.Dqb1, HlaTypingResolution.Untyped);
                    break;
                case "A":
                case "B":
                case "Drb1":
                    throw new Exception("Loci A, B, DRB1 cannot be untyped");
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            ScenarioContext.Current.Set(patientDataFactory);
        }

        [Given(@"the donor is of type (.*)")]
        [Given(@"the matching donor is of type (.*)")]
        public void GivenTheMatchingDonorIsOfDonorType(string donorType)
        {
            var metaDonorSelector = ScenarioContext.Current.Get<IPatientDataFactory>();
            metaDonorSelector.SetMatchDonorType(donorType);
            ScenarioContext.Current.Set(metaDonorSelector);
        }

        [Given(@"the donor is (.*) typed at (.*)")]
        [Given(@"the matching donor is (.*) typed at (.*)")]
        public void GivenTheMatchingDonorIsHlaTyped(string typingCategory, string locus)
        {
            var patientDataFactory = ScenarioContext.Current.Get<IPatientDataFactory>();
            patientDataFactory.SetMatchTypingCategories(typingCategory, locus);
            ScenarioContext.Current.Set(patientDataFactory);
        }

        [Given(@"the donor's allele string contains different antigen groups at (.*)")]
        [Given(@"the matching donor's allele string contains different antigen groups at (.*)")]
        public void GivenTheMatchingDonorsAlleleStringContainsDifferentAntigenGroupsAt(string locus)
        {
            var patientDataFactory = ScenarioContext.Current.Get<IPatientDataFactory>();
            patientDataFactory.SetAlleleStringShouldContainDifferentGroupsAt(locus);
            ScenarioContext.Current.Set(patientDataFactory);
        }

        [Given(@"the donor is homozygous at (.*)")]
        [Given(@"the matching donor is homozygous at (.*)")]
        public void GivenTheMatchingDonorIsHomozygousAt(string locus)
        {
            var patientDataFactory = ScenarioContext.Current.Get<IPatientDataFactory>();

            switch (locus)
            {
                case "locus A":
                    patientDataFactory.SetMatchingDonorHomozygousAtLocus(Locus.A);
                    break;
                case "locus B":
                    patientDataFactory.SetMatchingDonorHomozygousAtLocus(Locus.B);
                    break;
                case "locus C":
                    patientDataFactory.SetMatchingDonorHomozygousAtLocus(Locus.C);
                    break;
                case "locus DPB1":
                    patientDataFactory.SetMatchingDonorHomozygousAtLocus(Locus.Dpb1);
                    break;
                case "locus DQB1":
                    patientDataFactory.SetMatchingDonorHomozygousAtLocus(Locus.Dqb1);
                    break;
                case "locus DRB1":
                    patientDataFactory.SetMatchingDonorHomozygousAtLocus(Locus.Drb1);
                    break;
                case "all loci":
                    patientDataFactory.SetMatchingDonorHomozygousAtLocus(Locus.A);
                    patientDataFactory.SetMatchingDonorHomozygousAtLocus(Locus.B);
                    patientDataFactory.SetMatchingDonorHomozygousAtLocus(Locus.C);
                    patientDataFactory.SetMatchingDonorHomozygousAtLocus(Locus.Dpb1);
                    patientDataFactory.SetMatchingDonorHomozygousAtLocus(Locus.Dqb1);
                    patientDataFactory.SetMatchingDonorHomozygousAtLocus(Locus.Drb1);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            ScenarioContext.Current.Set(patientDataFactory);
        }

        [Given(@"the match orientation is (.*) at (.*)")]
        public void GivenTheMatchOrientationIs(string orientation, string locus)
        {
            var patientDataFactory = ScenarioContext.Current.Get<IPatientDataFactory>();
            patientDataFactory.SetMatchOrientationsAt(orientation, locus);
            ScenarioContext.Current.Set(patientDataFactory);
        }

        [Given(@"the donor is in registry: (.*)")]
        [Given(@"the matching donor is in registry: (.*)")]
        public void GivenTheMatchingDonorIsInRegistry(string registry)
        {
            var patientDataFactory = ScenarioContext.Current.Get<IPatientDataFactory>();
            patientDataFactory.SetMatchDonorRegistry(registry);
            ScenarioContext.Current.Set(patientDataFactory);
        }

        [Given(@"the match level is (.*)")]
        public void GivenTheMatchingDonorIsALevelMatch(string matchLevel)
        {
            var patientDataFactory = ScenarioContext.Current.Get<IPatientDataFactory>();
            patientDataFactory.SetMatchLevelAtAllLoci(matchLevel);
            ScenarioContext.Current.Set(patientDataFactory);
        }
        
        [Given(@"the donor has an allele with (.*) expression suffix at (.*)")]
        [Given(@"the matching donor has an allele with (.*) expression suffix at (.*)")]
        public void GivenTheMatchingDonorIsInRegistry(string expressionSuffixType, string locus)
        {
            var patientDataFactory = ScenarioContext.Current.Get<IPatientDataFactory>();
            patientDataFactory.SetExpressionSuffixAt(expressionSuffixType, locus);
            ScenarioContext.Current.Set(patientDataFactory);
        }
        
        [Given(@"the donor has a null allele at (.*) at (.*)")]
        [Given(@"the matching donor has a null allele at (.*) at (.*)")]
        public void GivenTheMatchingDonorHasANullAllele(string locus, string position)
        {
            var patientDataFactory = ScenarioContext.Current.Get<IPatientDataFactory>();
            patientDataFactory.SetNullAlleleAt(locus, position);
            ScenarioContext.Current.Set(patientDataFactory);
        }
    }
}