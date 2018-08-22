using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
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
        public static ISingleDonorPatientDataSelector SetMatchType(this ISingleDonorPatientDataSelector singleDonorPatientDataSelector,
            string matchType)
        {
            switch (matchType)
            {
                case "10/10":
                    singleDonorPatientDataSelector.SetAsTenOutOfTenMatch();
                    break;
                case "8/8":
                    singleDonorPatientDataSelector.SetAsEightOutOfEightMatch();
                    break;
                case "6/6":
                    singleDonorPatientDataSelector.SetAsSixOutOfSixMatch();
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return singleDonorPatientDataSelector;
        }

        public static ISingleDonorPatientDataSelector SetMismatches(this ISingleDonorPatientDataSelector singleDonorPatientDataSelector,
            string mismatchType, string locus)
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
                    singleDonorPatientDataSelector.SetMismatchesAtLocus(mismatchCount, Locus.A);
                    break;
                case "locus B":
                    singleDonorPatientDataSelector.SetMismatchesAtLocus(mismatchCount, Locus.B);
                    break;
                case "locus C":
                    singleDonorPatientDataSelector.SetMismatchesAtLocus(mismatchCount, Locus.C);
                    break;
                case "locus Dpb1":
                    singleDonorPatientDataSelector.SetMismatchesAtLocus(mismatchCount, Locus.Dpb1);
                    break;
                case "locus Dqb1":
                    singleDonorPatientDataSelector.SetMismatchesAtLocus(mismatchCount, Locus.Dqb1);
                    break;
                case "locus Drb1":
                    singleDonorPatientDataSelector.SetMismatchesAtLocus(mismatchCount, Locus.Drb1);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return singleDonorPatientDataSelector;
        }

        public static ISingleDonorPatientDataSelector SetMatchDonorType(this ISingleDonorPatientDataSelector singleDonorPatientDataSelector,
            string matchDonorType)
        {
            switch (matchDonorType)
            {
                case "adult":
                    singleDonorPatientDataSelector.SetMatchingDonorType(DonorType.Adult);
                    break;
                case "cord":
                    singleDonorPatientDataSelector.SetMatchingDonorType(DonorType.Cord);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return singleDonorPatientDataSelector;
        }

        public static ISingleDonorPatientDataSelector SetMatchTypingCategories(
            this ISingleDonorPatientDataSelector singleDonorPatientDataSelector,
            string typingCategory,
            string locus
        )
        {
            switch (locus)
            {
                case "each locus":
                    singleDonorPatientDataSelector = SetTypingCategoryAtAllLoci(singleDonorPatientDataSelector, typingCategory);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return singleDonorPatientDataSelector;
        }

        public static ISingleDonorPatientDataSelector SetMatchDonorRegistry(this ISingleDonorPatientDataSelector singleDonorPatientDataSelector,
            string registry)
        {
            switch (registry)
            {
                case "Anthony Nolan":
                    singleDonorPatientDataSelector.SetMatchingRegistry(RegistryCode.AN);
                    break;
                case "DKMS":
                    singleDonorPatientDataSelector.SetMatchingRegistry(RegistryCode.DKMS);
                    break;
                case "BBMR":
                case "NHSBT":
                    singleDonorPatientDataSelector.SetMatchingRegistry(RegistryCode.NHSBT);
                    break;
                case "WBMDR":
                case "WBS":
                    singleDonorPatientDataSelector.SetMatchingRegistry(RegistryCode.WBS);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return singleDonorPatientDataSelector;
        }

        public static ISingleDonorPatientDataSelector SetMatchLevelAtAllLoci(this ISingleDonorPatientDataSelector singleDonorPatientDataSelector,
            string matchLevel)
        {
            switch (matchLevel)
            {
                case "p-group":
                    singleDonorPatientDataSelector.SetAsMatchLevelAtAllLoci(MatchLevel.PGroup);
                    break;
                case "g-group":
                    singleDonorPatientDataSelector.SetAsMatchLevelAtAllLoci(MatchLevel.GGroup);
                    break;
                case "three field (different fourth field)":
                    singleDonorPatientDataSelector.SetAsMatchLevelAtAllLoci(MatchLevel.FirstThreeFieldAllele);
                    break;
                case "two field (different third field)":
                    singleDonorPatientDataSelector.SetAsMatchLevelAtAllLoci(MatchLevel.FirstTwoFieldAllele);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return singleDonorPatientDataSelector;
        }

        private static ISingleDonorPatientDataSelector SetTypingCategoryAtAllLoci(this ISingleDonorPatientDataSelector singleDonorPatientDataSelector,
            string typingCategory)
        {
            switch (typingCategory)
            {
                case "differently":
                    // Mixed resolution must have 4-field TGS alleles, as one of the resolution options is three field truncated
                    singleDonorPatientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    foreach (var resolution in TestCaseTypingResolutions.DifferentLociResolutions)
                    {
                        singleDonorPatientDataSelector.SetMatchingTypingResolutionAtLocus(resolution.Key, resolution.Value);
                    }

                    break;
                case "TGS":
                    singleDonorPatientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.Arbitrary);
                    singleDonorPatientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.Tgs);
                    break;
                case "TGS (four field)":
                case "TGS (four-field)":
                    singleDonorPatientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    singleDonorPatientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.Tgs);
                    break;
                case "TGS (three field)":
                    singleDonorPatientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.ThreeFieldAllele);
                    singleDonorPatientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.Tgs);
                    break;
                case "TGS (two field)":
                    singleDonorPatientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.TwoFieldAllele);
                    singleDonorPatientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.Tgs);
                    break;
                case "three field truncated allele":
                    singleDonorPatientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    singleDonorPatientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.ThreeFieldTruncatedAllele);
                    break;
                case "two field truncated allele":
                    singleDonorPatientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    singleDonorPatientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.TwoFieldTruncatedAllele);
                    break;
                case "XX code":
                    singleDonorPatientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.Arbitrary);
                    singleDonorPatientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.XxCode);
                    break;
                case "NMDP code":
                    singleDonorPatientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.Arbitrary);
                    singleDonorPatientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.NmdpCode);
                    break;
                case "serology":
                    singleDonorPatientDataSelector.SetFullMatchingTgsCategory(TgsHlaTypingCategory.Arbitrary);
                    singleDonorPatientDataSelector.SetFullMatchingTypingResolution(HlaTypingResolution.Serology);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return singleDonorPatientDataSelector;
        }
    }
}