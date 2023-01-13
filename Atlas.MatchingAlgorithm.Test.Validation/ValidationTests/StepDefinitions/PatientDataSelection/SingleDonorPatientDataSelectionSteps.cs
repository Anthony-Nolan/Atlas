using System;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.PatientFactories;
using Atlas.MatchingAlgorithm.Test.Validation.ValidationTests.StepDefinitions.InputParsers;
using TechTalk.SpecFlow;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationTests.StepDefinitions.PatientDataSelection
{
    /// <summary>
    /// Contains step definitions for selecting patient data when a single patient corresponds to a single donor
    /// </summary>
    [Binding]
    public class SingleDonorPatientDataSelectionSteps
    {
        private readonly ScenarioContext scenarioContext;

        public SingleDonorPatientDataSelectionSteps(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        [Given(@"a patient and a donor")]
        public void GivenAPatientAndADonor()
        {
        }

        [Given(@"a patient has a match")]
        public void GivenAPatientHasAMatch()
        {
        }

        [Given(@"the patient is untyped at locus (.*)")]
        [Given(@"the patient is untyped at Locus (.*)")]
        public void GivenThePatientIsUntypedAt(string locus)
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();

            switch (locus)
            {
                case "C":
                    patientDataFactory.SetPatientUntypedAtLocus(Locus.C);
                    break;
                case "Dpb1":
                case "DPB1":
                    patientDataFactory.SetPatientUntypedAtLocus(Locus.Dpb1);
                    break;
                case "Dqb1":
                case "DQB1":
                    patientDataFactory.SetPatientUntypedAtLocus(Locus.Dqb1);
                    break;
                case "A":
                case "B":
                case "Drb1":
                case "DRB1":
                    throw new Exception("Loci A, B, DRB1 cannot be untyped");
                default:
                    scenarioContext.Pending();
                    break;
            }

            scenarioContext.Set(patientDataFactory);
        }
        
        [Given(@"the patient is (.*) typed at (.*)")]
        public void GivenThePatientIsUntypedAt(string typingCategory, string locus)
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();
            patientDataFactory.SetPatientTypingCategoryAt(typingCategory, locus);
            scenarioContext.Set(patientDataFactory);
        }

        [Given(@"the patient is homozygous at (.*)")]
        public void GivenThePatientIsHomozygousAt(string locusString)
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();
            var loci = LocusParser.ParseLoci(locusString);

            foreach (var locus in loci)
            {
                patientDataFactory.SetPatientHomozygousAtLocus(locus);
            }

            scenarioContext.Set(patientDataFactory);
        }

        [Given(@"the donor is a (.*) match")]
        [Given(@"the matching donor is a (.*) match")]
        public void GivenTheMatchingDonorIsOfMatchType(string matchType)
        {
            var patientDataFactory = (PatientDataFactory) scenarioContext.Get<IPatientDataFactory>();
            patientDataFactory.SetMatchType(matchType);
            scenarioContext.Set(patientDataFactory);
        }

        [Given(@"the donor has a (.*) mismatch at (.*)")]
        [Given(@"the matching donor has a (.*) mismatch at (.*)")]
        public void GivenTheMatchingDonorHasAMismatchAt(string mismatchType, string locus)
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();
            patientDataFactory.SetMismatches(mismatchType, locus);
            scenarioContext.Set(patientDataFactory);
        }

        [Given(@"the donor has a mismatch at (.*) at (.*)")]
        [Given(@"the matching donor has a mismatch at (.*) at (.*)")]
        public void GivenTheMatchingDonorHasAMismatchAtPosition(string locus, string position)
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();
            patientDataFactory.SetMismatchAt(locus, position);
            scenarioContext.Set(patientDataFactory);
        }

        [Given(@"the donor is untyped at (.*)")]
        [Given(@"the matching donor is untyped at (.*)")]
        public void GivenTheMatchingDonorIsUntypedAt(string locusString)
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();
            var loci = LocusParser.ParseLoci(locusString).ToList();

            var lociWithMandatoryTyping = new[] {Locus.A, Locus.B, Locus.Drb1};
            if (loci.Any(l => lociWithMandatoryTyping.Contains(l)))
            {
                throw new Exception("Loci A, B, DRB1 cannot be untyped");
            }

            foreach (var locus in loci)
            {
                patientDataFactory.UpdateMatchingDonorTypingResolutionsAtLocus(locus, HlaTypingResolution.Untyped);
            }

            scenarioContext.Set(patientDataFactory);
        }

        [Given(@"the donor is of type (.*)")]
        [Given(@"the matching donor is of type (.*)")]
        public void GivenTheMatchingDonorIsOfDonorType(string donorType)
        {
            var metaDonorSelector = scenarioContext.Get<IPatientDataFactory>();
            metaDonorSelector.SetMatchDonorType(donorType);
            scenarioContext.Set(metaDonorSelector);
        }

        [Given(@"the donor is (.*) typed at (.*)")]
        [Given(@"the matching donor is (.*) typed at (.*)")]
        public void GivenTheMatchingDonorIsHlaTyped(string typingCategory, string locus)
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();
            patientDataFactory.SetMatchTypingCategories(typingCategory, locus);
            scenarioContext.Set(patientDataFactory);
        }

        [Given(@"the donor's allele string contains different antigen groups at (.*)")]
        [Given(@"the matching donor's allele string contains different antigen groups at (.*)")]
        public void GivenTheMatchingDonorsAlleleStringContainsDifferentAntigenGroupsAt(string locus)
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();
            patientDataFactory.SetAlleleStringShouldContainDifferentGroupsAt(locus);
            scenarioContext.Set(patientDataFactory);
        }

        [Given(@"the donor is homozygous at (.*)")]
        [Given(@"the matching donor is homozygous at (.*)")]
        public void GivenTheMatchingDonorIsHomozygousAt(string locus)
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();

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
                    scenarioContext.Pending();
                    break;
            }

            scenarioContext.Set(patientDataFactory);
        }

        [Given(@"the match orientation is (.*) at (.*)")]
        public void GivenTheMatchOrientationIs(string orientation, string locus)
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();
            patientDataFactory.SetMatchOrientationsAt(orientation, locus);
            scenarioContext.Set(patientDataFactory);
        }

        [Given(@"the match level is (.*)")]
        public void GivenTheMatchingDonorIsALevelMatch(string matchLevel)
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();
            patientDataFactory.SetMatchLevelAtAllLoci(matchLevel);
            scenarioContext.Set(patientDataFactory);
        }
        
        [Given(@"the donor has an allele with (.*) expression suffix at (.*)")]
        [Given(@"the matching donor has an allele with (.*) expression suffix at (.*)")]
        public void GivenTheMatchingDonorHasAnAlleleWithExpressionSuffix(string expressionSuffixType, string locus)
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();
            patientDataFactory.SetExpressionSuffixAt(expressionSuffixType, locus);
            scenarioContext.Set(patientDataFactory);
        }
        
        [Given(@"the donor has a null allele at (.*) at (.*)")]
        [Given(@"the matching donor has a null allele at (.*) at (.*)")]
        public void GivenTheMatchingDonorHasANullAllele(string locus, string position)
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();
            patientDataFactory.SetNullAlleleAt(locus, position);
            scenarioContext.Set(patientDataFactory);
        }
        
        [Given(@"the patient has a null allele at (.*) at (.*)")]
        [Given(@"the patient has a different null allele at (.*) at (.*)")]
        public void GivenThePatientHasADifferentNullAllele(string locus, string position)
        {
            var patientDataFactory = scenarioContext.Get<IPatientDataFactory>();
            patientDataFactory.SetPatientNonMatchingNullAlleleAt(locus, position);
            scenarioContext.Set(patientDataFactory);
        }
    }
}