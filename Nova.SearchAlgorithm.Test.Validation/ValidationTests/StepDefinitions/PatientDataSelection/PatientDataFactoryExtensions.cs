using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources.SpecificTestCases;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.PatientFactories;
using System.Collections.Generic;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions.PatientDataSelection
{
    /// <summary>
    /// Contains a set of extension methods that allow for easy manipulation of a patient data factory based on string inputs from the test steps.
    /// This logic should not be moved to the selctor itself, as that should remain strongly typed.
    /// </summary>
    public static class PatientDataFactoryExtensions
    {
        public static IPatientDataFactory SetMatchType(this IPatientDataFactory factory, string matchType)
        {
            switch (matchType)
            {
                case "10/10":
                    factory.SetAsTenOutOfTenMatch();
                    break;
                case "8/8":
                    factory.SetAsEightOutOfEightMatch();
                    break;
                case "6/6":
                    factory.SetAsSixOutOfSixMatch();
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return factory;
        }

        public static IPatientDataFactory SetMismatches(this IPatientDataFactory factory, string mismatchType, string locusType)
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

            var loci = ParseLoci(locusType);

            foreach (var locus in loci)
            {
                factory.SetMismatchesAtLocus(mismatchCount, locus);
            }

            return factory;
        }

        public static IPatientDataFactory SetMatchTypingCategories(this IPatientDataFactory factory, string typingCategory, string locus)
        {
            switch (locus)
            {
                case "each locus":
                    factory = SetDonorTypingCategoryAtAllLoci(factory, typingCategory);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return factory;
        }

        public static IPatientDataFactory SetMatchOrientationsAt(this IPatientDataFactory factory, string orientationString, string locusType)
        {
            var loci = ParseLoci(locusType);
            var orientation = ParseOrientation(orientationString);

            foreach (var locus in loci)
            {
                factory.SetMatchOrientationAtLocus(locus, orientation);
            }

            return factory;
        }

        public static IPatientDataFactory SetMatchLevelAtAllLoci(this IPatientDataFactory factory, string matchLevel)
        {
            switch (matchLevel)
            {
                case "p-group":
                    factory.SetAsMatchLevelAtAllLoci(MatchLevel.PGroup);
                    break;
                case "g-group":
                    factory.SetAsMatchLevelAtAllLoci(MatchLevel.GGroup);
                    break;
                case "protein":
                    factory.SetAsMatchLevelAtAllLoci(MatchLevel.Protein);
                    break;
                case "cdna":
                case "cDna":
                case "CDNA":
                case "cDNA":
                    factory.SetAsMatchLevelAtAllLoci(MatchLevel.CDna);
                    break;
                case "gdna":
                case "gDna":
                case "gDNA":
                case "GDNA":
                    factory.SetAsMatchLevelAtAllLoci(MatchLevel.Allele);
                    break;
                case "three field (different fourth field)":
                    factory.SetAsMatchLevelAtAllLoci(MatchLevel.FirstThreeFieldAllele);
                    break;
                case "two field (different third field)":
                    factory.SetAsMatchLevelAtAllLoci(MatchLevel.FirstTwoFieldAllele);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return factory;
        }

        public static IPatientDataFactory SetMatchDonorType(this IPatientDataFactory factory, string matchDonorType)
        {
            switch (matchDonorType)
            {
                case "adult":
                    factory.SetMatchingDonorType(DonorType.Adult);
                    break;
                case "cord":
                    factory.SetMatchingDonorType(DonorType.Cord);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return factory;
        }

        public static IPatientDataFactory SetMatchDonorRegistry(this IPatientDataFactory factory, string registry)
        {
            switch (registry)
            {
                case "Anthony Nolan":
                    factory.SetMatchingRegistry(RegistryCode.AN);
                    break;
                case "DKMS":
                    factory.SetMatchingRegistry(RegistryCode.DKMS);
                    break;
                case "BBMR":
                case "NHSBT":
                    factory.SetMatchingRegistry(RegistryCode.NHSBT);
                    break;
                case "WBMDR":
                case "WBS":
                    factory.SetMatchingRegistry(RegistryCode.WBS);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return factory;
        }

        public static IPatientDataFactory SetAlleleStringShouldContainDifferentGroupsAt(this IPatientDataFactory factory,string locusString)
        {
            switch (locusString)
            {
                case "each locus":
                    foreach (var locus in LocusHelpers.AllLoci())
                    {
                        factory.SetAlleleStringShouldContainDifferentGroupsAtLocus(locus);
                    }

                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return factory;
        }

        public static IPatientDataFactory SetExpressionSuffixAt(this IPatientDataFactory factory, string expressionSuffix, string locusType)
        {
            var loci = ParseLoci(locusType);

            switch (expressionSuffix)
            {
                case "any (non-null)":

                    foreach (var locus in loci)
                    {
                        factory.SetHasNonNullExpressionSuffixAtLocus(locus);
                    }

                    break;
                case "an 'S'":
                case "an 'Q'":
                case "an 'L'":
                    throw new InvalidTestDataException("Cannot select a specific non-null expression suffix - this functionality will need adding");
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return factory;
        }

        public static IPatientDataFactory SetPatientTypingCategoryAt(this IPatientDataFactory factory, string typingCategory, string locusType)
        {
            var loci = ParseLoci(locusType);
            var typingResolution = ParsePatientTypingResolution(typingCategory);
            
            foreach (var locus in loci)
            {
                factory.SetPatientTypingResolutionAtLocus(locus, typingResolution);
            }

            return factory;
        }

        private static HlaTypingResolution ParsePatientTypingResolution(string typingResolution)
        {
            switch (typingResolution)
            {
                case "serology":
                    return HlaTypingResolution.Serology;
                default:
                    ScenarioContext.Current.Pending();
                    return HlaTypingResolution.Tgs;
            }
        }

        private static IEnumerable<Locus> ParseLoci(string locus)
        {
            switch (locus)
            {
                case "each locus":
                case "all loci":
                    return LocusHelpers.AllLoci();
                case "locus A":
                    return new[] {Locus.A};
                case "locus B":
                    return new[] {Locus.B};
                case "locus C":
                    return new[] {Locus.C};
                case "locus Dpb1":
                case "locus DPB1":
                    return new[] {Locus.Dpb1};
                case "locus Dqb1":
                case "locus DQB1":
                    return new[] {Locus.Dqb1};
                case "locus Drb1":
                case "locus DRB1":
                    return new[] {Locus.Drb1};
                default:
                    ScenarioContext.Current.Pending();
                    return new List<Locus>();
            }
        }

        private static MatchOrientation ParseOrientation(string orientation)
        {
            switch (orientation)
            {
                case "cross":
                    return MatchOrientation.Cross;
                case "direct":
                    return MatchOrientation.Direct;
                default:
                    ScenarioContext.Current.Pending();
                    return MatchOrientation.Arbitrary;
            }
        }

        private static IPatientDataFactory SetDonorTypingCategoryAtAllLoci(this IPatientDataFactory factory, string typingCategory)
        {
            switch (typingCategory)
            {
                case "differently":
                    // Mixed resolution must have 4-field TGS alleles, as one of the resolution options is three field truncated
                    factory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    foreach (var resolution in TestCaseTypingResolutions.DifferentLociResolutions)
                    {
                        factory.UpdateMatchingDonorTypingResolutionsAtLocus(resolution.Key, resolution.Value);
                    }

                    break;
                case "TGS":
                    factory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.Arbitrary);
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.Tgs);
                    break;
                case "TGS (four field)":
                case "TGS (four-field)":
                    factory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.Tgs);
                    break;
                case "TGS (three field)":
                    factory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.ThreeFieldAllele);
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.Tgs);
                    break;
                case "TGS (two field)":
                    factory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.TwoFieldAllele);
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.Tgs);
                    break;
                case "three field truncated allele":
                    factory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.ThreeFieldTruncatedAllele);
                    break;
                case "two field truncated allele":
                    factory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.TwoFieldTruncatedAllele);
                    break;
                case "XX code":
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.XxCode);
                    break;
                case "NMDP code":
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.NmdpCode);
                    break;
                case "serology":
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.Serology);
                    break;
                case "allele string":
                case "allele string (of names)":
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.AlleleStringOfNames);
                    break;
                case "allele string (of subtypes)":
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.AlleleStringOfSubtypes);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return factory;
        }
    }
}