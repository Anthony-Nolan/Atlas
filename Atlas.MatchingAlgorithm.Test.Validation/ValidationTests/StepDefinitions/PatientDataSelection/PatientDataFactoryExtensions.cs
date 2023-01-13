using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.SpecificTestCases;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.PatientFactories;
using Atlas.MatchingAlgorithm.Test.Validation.ValidationTests.StepDefinitions.InputParsers;
using TechTalk.SpecFlow;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationTests.StepDefinitions.PatientDataSelection
{
    /// <summary>
    /// Contains a set of extension methods that allow for easy manipulation of a patient data factory based on string inputs from the test steps.
    /// This logic should not be moved to the selector itself, as that should remain strongly typed.
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
                    throw new PendingStepException();
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
                    throw new PendingStepException();
            }

            var loci = LocusParser.ParseLoci(locusType);

            foreach (var locus in loci)
            {
                if (mismatchCount > 0)
                {
                    factory.SetMismatchAtPosition(locus, LocusPosition.One);
                }

                if (mismatchCount > 1)
                {
                    factory.SetMismatchAtPosition(locus, LocusPosition.Two);
                }
            }

            return factory;
        }

        public static IPatientDataFactory SetMismatchAt(this IPatientDataFactory factory, string locusType, string positionType)
        {
            var loci = LocusParser.ParseLoci(locusType);
            var positions = PositionParser.ParsePositions(positionType).ToList();
            
            foreach (var locus in loci)
            {
                foreach (var position in positions)
                {
                    factory.SetMismatchAtPosition(locus, position);
                }
            }

            return factory;
        }

        public static IPatientDataFactory SetMatchTypingCategories(this IPatientDataFactory factory, string typingCategory, string locus)
        {
            switch (locus)
            {
                case "each locus":
                case "all loci":
                    factory = SetDonorTypingCategoryAtAllLoci(factory, typingCategory);
                    break;
                default:
                    throw new PendingStepException();
            }

            return factory;
        }

        public static IPatientDataFactory SetMatchOrientationsAt(this IPatientDataFactory factory, string orientationString, string locusType)
        {
            var loci = LocusParser.ParseLoci(locusType);
            var orientation = OrientationParser.ParseOrientation(orientationString);

            foreach (var locus in loci)
            {
                factory.SetMatchOrientationAtLocus(locus, orientation);
            }

            return factory;
        }

        public static IPatientDataFactory SetMatchLevelAtAllLoci(this IPatientDataFactory factory, string matchLevel)
        {
            switch (matchLevel.ToLower())
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
                    factory.SetAsMatchLevelAtAllLoci(MatchLevel.CDna);
                    break;
                case "gdna":
                    factory.SetAsMatchLevelAtAllLoci(MatchLevel.Allele);
                    break;
                case "three field (different fourth field)":
                    factory.SetAsMatchLevelAtAllLoci(MatchLevel.FirstThreeFieldAllele);
                    break;
                case "two field (different third field)":
                    factory.SetAsMatchLevelAtAllLoci(MatchLevel.FirstTwoFieldAllele);
                    break;
                default:
                    throw new PendingStepException();
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
                    throw new PendingStepException();
            }

            return factory;
        }

        public static IPatientDataFactory SetAlleleStringShouldContainDifferentGroupsAt(this IPatientDataFactory factory, string locusString)
        {
            switch (locusString)
            {
                case "each locus":
                    foreach (var locus in EnumerateValues<Locus>())
                    {
                        factory.SetAlleleStringShouldContainDifferentGroupsAtLocus(locus);
                    }

                    break;
                default:
                    throw new PendingStepException();
            }

            return factory;
        }

        public static IPatientDataFactory SetExpressionSuffixAt(this IPatientDataFactory factory, string expressionSuffix, string locusType)
        {
            var loci = LocusParser.ParseLoci(locusType);

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
                    throw new PendingStepException();
            }

            return factory;
        }

        public static IPatientDataFactory SetNullAlleleAt(this IPatientDataFactory factory, string locusType, string positionType)
        {
            var loci = LocusParser.ParseLoci(locusType);
            var positions = PositionParser.ParsePositions(positionType).ToList();

            foreach (var locus in loci)
            {
                foreach (var position in positions)
                {
                    factory.SetHasNullAlleleAtPosition(locus, position);
                }
            }

            return factory;
        }

        public static IPatientDataFactory SetPatientNonMatchingNullAlleleAt(this IPatientDataFactory factory, string locusType, string positionType)
        {
            var loci = LocusParser.ParseLoci(locusType);
            var positions = PositionParser.ParsePositions(positionType).ToList();

            foreach (var locus in loci)
            {
                foreach (var position in positions)
                {
                    factory.SetPatientNonMatchingNullAlleleAtPosition(locus, position);
                }
            }

            return factory;
        }

        public static IPatientDataFactory SetPatientTypingCategoryAt(this IPatientDataFactory factory, string typingCategory, string locusType)
        {
            var loci = LocusParser.ParseLoci(locusType);
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
                case "unambiguously":
                    return HlaTypingResolution.Unambiguous;
                case "ambiguously (single P group)":
                    return HlaTypingResolution.AlleleStringOfNamesWithSinglePGroup;
                case "ambiguously (multiple P groups)":
                    return HlaTypingResolution.AlleleStringOfNamesWithMultiplePGroups;
                default:
                    throw new PendingStepException();
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
                case "'TGS derived data'":
                    factory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.Arbitrary);
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.Tgs);
                    break;
                case "'TGS derived data at four-field resolution'":
                    factory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.Tgs);
                    break;
                case "'TGS derived data at three-field resolution'":
                    factory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.ThreeFieldAllele);
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.Tgs);
                    break;
                case "'TGS derived data at two-field resolution'":
                    factory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.TwoFieldAllele);
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.Tgs);
                    break;
                case "three field truncated allele":
                case "three-field truncated allele":
                    factory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.ThreeFieldTruncatedAllele);
                    break;
                case "two field truncated allele":
                case "two-field truncated allele":
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
                case "P-group":
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.PGroup);
                    break;
                case "G-group":
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.GGroup);
                    break;
                case "allele string":
                case "allele string (of names)":
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.AlleleStringOfNames);
                    break;
                case "allele string (of subtypes)":
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.AlleleStringOfSubtypes);
                    break;
                case "unambiguously":
                    // Only 4-field TGS alleles are guaranteed to be 'unambiguous' typings
                    factory.SetFullMatchingTgsCategory(TgsHlaTypingCategory.FourFieldAllele);
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.Unambiguous);
                    break;
                case "ambiguously (single P group)":
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.AlleleStringOfNamesWithSinglePGroup);
                    break;
                case "ambiguously (multiple P groups)":
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.AlleleStringOfNamesWithMultiplePGroups);
                    break;
                case "arbitrarily":
                    factory.UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution.Arbitrary);
                    break;
                default:
                    throw new PendingStepException();
            }

            return factory;
        }
    }
}