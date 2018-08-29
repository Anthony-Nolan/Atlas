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
    /// Contains a set of extension methods that allow for easy manipulation of a patient data factory based on string inputs from the test steps.
    /// This logic should not be moved to the selctor itself, as that should remain strongly typed.
    /// </summary>
    public static class PatientDataFactoryExtensions
    {
        public static IPatientDataFactory SetMatchType(this IPatientDataFactory patientDataFactory, string matchType)
        {
            switch (matchType)
            {
                case "10/10":
                    patientDataFactory.SetAsTenOutOfTenMatch();
                    break;
                case "8/8":
                    patientDataFactory.SetAsEightOutOfEightMatch();
                    break;
                case "6/6":
                    patientDataFactory.SetAsSixOutOfSixMatch();
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return patientDataFactory;
        }

        public static IPatientDataFactory SetMismatches(this IPatientDataFactory patientDataFactory, string mismatchType, string locus)
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
                    patientDataFactory.SetMismatchesAtLocus(mismatchCount, Locus.A);
                    break;
                case "locus B":
                    patientDataFactory.SetMismatchesAtLocus(mismatchCount, Locus.B);
                    break;
                case "locus C":
                    patientDataFactory.SetMismatchesAtLocus(mismatchCount, Locus.C);
                    break;
                case "locus Dpb1":
                case "locus DPB1":
                    patientDataFactory.SetMismatchesAtLocus(mismatchCount, Locus.Dpb1);
                    break;
                case "locus Dqb1":
                case "locus DQB1":
                    patientDataFactory.SetMismatchesAtLocus(mismatchCount, Locus.Dqb1);
                    break;
                case "locus Drb1":
                case "locus DRB1":
                    patientDataFactory.SetMismatchesAtLocus(mismatchCount, Locus.Drb1);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return patientDataFactory;
        }

        public static IPatientDataFactory SetMatchTypingCategories(this IPatientDataFactory patientDataFactory, string typingCategory, string locus)
        {
            switch (locus)
            {
                case "each locus":
                    patientDataFactory = SetTypingCategoryAtAllLoci(patientDataFactory, typingCategory);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return patientDataFactory;
        }

        public static IPatientDataFactory SetMatchLevelAtAllLoci(this IPatientDataFactory patientDataFactory, string matchLevel)
        {
            switch (matchLevel)
            {
                case "p-group":
                    patientDataFactory.SetAsMatchLevelAtAllLoci(MatchLevel.PGroup);
                    break;
                case "g-group":
                    patientDataFactory.SetAsMatchLevelAtAllLoci(MatchLevel.GGroup);
                    break;
                case "three field (different fourth field)":
                    patientDataFactory.SetAsMatchLevelAtAllLoci(MatchLevel.FirstThreeFieldAllele);
                    break;
                case "two field (different third field)":
                    patientDataFactory.SetAsMatchLevelAtAllLoci(MatchLevel.FirstTwoFieldAllele);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return patientDataFactory;
        }

        private static IPatientDataFactory SetTypingCategoryAtAllLoci(this IPatientDataFactory patientDataFactory, string typingCategory)
        {
            switch (typingCategory)
            {
                case "differently":
                    // Mixed resolution must have 4-field TGS alleles, as one of the resolution options is three field truncated
                    patientDataFactory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    foreach (var resolution in TestCaseTypingResolutions.DifferentLociResolutions)
                    {
                        patientDataFactory.UpdateMatchingDonorTypingResolutionsAtLocus(resolution.Key, resolution.Value);
                    }

                    break;
                case "TGS":
                    patientDataFactory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.Arbitrary);
                    patientDataFactory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.Tgs);
                    break;
                case "TGS (four field)":
                case "TGS (four-field)":
                    patientDataFactory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    patientDataFactory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.Tgs);
                    break;
                case "TGS (three field)":
                    patientDataFactory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.ThreeFieldAllele);
                    patientDataFactory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.Tgs);
                    break;
                case "TGS (two field)":
                    patientDataFactory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.TwoFieldAllele);
                    patientDataFactory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.Tgs);
                    break;
                case "three field truncated allele":
                    patientDataFactory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    patientDataFactory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.ThreeFieldTruncatedAllele);
                    break;
                case "two field truncated allele":
                    patientDataFactory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    patientDataFactory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.TwoFieldTruncatedAllele);
                    break;
                case "XX code":
                    patientDataFactory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.XxCode);
                    break;
                case "NMDP code":
                    patientDataFactory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.NmdpCode);
                    break;
                case "serology":
                    patientDataFactory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.Serology);
                    break;
                case "allele string":
                case "allele string (of names)":
                    patientDataFactory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.AlleleStringOfNames);
                    break;
                case "allele string (of subtypes)":
                    patientDataFactory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.AlleleStringOfSubtypes);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return patientDataFactory;
        }

        public static IPatientDataFactory SetMatchDonorType(this IPatientDataFactory singleDonorPatientDataSelector, string matchDonorType)
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

        public static IPatientDataFactory SetMatchDonorRegistry(this IPatientDataFactory singleDonorPatientDataSelector, string registry)
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

        public static IPatientDataFactory SetAlleleStringShouldContainDifferentGroupsAt(
            this IPatientDataFactory patientDataFactory,
            string locusString
        )
        {
            switch (locusString)
            {
                case "each locus":
                    foreach (var locus in LocusHelpers.AllLoci())
                    {
                        patientDataFactory.SetAlleleStringShouldContainDifferentGroupsAtLocus(locus);
                    }

                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return patientDataFactory;
        }
    }
}