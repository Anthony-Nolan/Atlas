using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources.SpecificTestCases;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions
{
    /// <summary>
    /// Contains a set of extension methods that allow for easy manipulation of a patient data selector based on string inputs from the test steps.
    /// This logic should not be moved to the selctor itself, as that should remain strongly typed.
    /// </summary>
    public static class PatientDataSelectorExtensions
    {
        public static IPatientDataSelector SetMatchType(this IPatientDataSelector patientDataSelector, string matchType)
        {
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

            return patientDataSelector;
        }

        public static IPatientDataSelector SetMismatches(this IPatientDataSelector patientDataSelector, string mismatchType, string locus)
        {
            var mismatchCount = 0;
            switch (mismatchType)
            {
                case "single":
                    mismatchCount = 1;
                    break;
                case "double":
                    mismatchCount = 2;
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            switch (locus)
            {
                case "locus A":
                    patientDataSelector.SetMismatchesAtLocus(mismatchCount, Locus.A);
                    break;
                case "locus B":
                    patientDataSelector.SetMismatchesAtLocus(mismatchCount, Locus.B);
                    break;
                case "locus C":
                    patientDataSelector.SetMismatchesAtLocus(mismatchCount, Locus.C);
                    break;
                case "locus Dpb1":
                    patientDataSelector.SetMismatchesAtLocus(mismatchCount, Locus.Dpb1);
                    break;
                case "locus Dqb1":
                    patientDataSelector.SetMismatchesAtLocus(mismatchCount, Locus.Dqb1);
                    break;
                case "locus Drb1":
                    patientDataSelector.SetMismatchesAtLocus(mismatchCount, Locus.Drb1);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return patientDataSelector;
        }

        public static IPatientDataSelector SetMatchDonorType(this IPatientDataSelector patientDataSelector, string matchDonorType)
        {
            switch (matchDonorType)
            {
                case "adult":
                    patientDataSelector.SetMatchingDonorType(DonorType.Adult);
                    break;
                case "cord":
                    patientDataSelector.SetMatchingDonorType(DonorType.Cord);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return patientDataSelector;
        }

        public static IPatientDataSelector SetMatchTypingCategories(
            this IPatientDataSelector patientDataSelector,
            string typingCategory,
            string locus
        )
        {
            switch (locus)
            {
                case "each locus":
                    patientDataSelector = SetTypingCategoryAtAllLoci(patientDataSelector, typingCategory);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return patientDataSelector;
        }

        public static IPatientDataSelector SetMatchDonorRegistry(this IPatientDataSelector patientDataSelector, string registry)
        {
            switch (registry)
            {
                case "Anthony Nolan":
                    patientDataSelector.SetMatchingRegistry(RegistryCode.AN);
                    break;
                case "DKMS":
                    patientDataSelector.SetMatchingRegistry(RegistryCode.DKMS);
                    break;
                case "BBMR":
                case "NHSBT":
                    patientDataSelector.SetMatchingRegistry(RegistryCode.NHSBT);
                    break;
                case "WBMDR":
                case "WBS":
                    patientDataSelector.SetMatchingRegistry(RegistryCode.WBS);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return patientDataSelector;
        }

        private static IPatientDataSelector SetTypingCategoryAtAllLoci(this IPatientDataSelector patientDataSelector, string typingCategory)
        {
            switch (typingCategory)
            {
                case "differently":
                    // Mixed resolution must have 4-field TGS alleles, as one of the resolution options is three field truncated
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
                case "TGS (four-field)":
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
    }
}