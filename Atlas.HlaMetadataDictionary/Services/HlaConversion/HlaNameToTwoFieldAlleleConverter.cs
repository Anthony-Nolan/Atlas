using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalExceptions;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.MultipleAlleleCodeDictionary;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;

namespace Atlas.HlaMetadataDictionary.Services.HlaConversion
{
    internal interface IHlaNameToTwoFieldAlleleConverter
    {
        Task<IReadOnlyCollection<string>> ConvertHla(
            Locus locus, string hlaName, ExpressionSuffixBehaviour behaviour, string hlaNomenclatureVersion);
    }

    internal enum ExpressionSuffixBehaviour
    {
        Include,
        Exclude
    }

    internal class HlaNameToTwoFieldAlleleConverter : IHlaNameToTwoFieldAlleleConverter
    {
        private readonly IHlaCategorisationService hlaCategorisationService;
        private readonly IAlleleNamesExtractor alleleNamesExtractor;
        private readonly IMacDictionary macDictionary;
        private readonly IAlleleGroupExpander groupExpander;

        public HlaNameToTwoFieldAlleleConverter(
            IHlaCategorisationService hlaCategorisationService,
            IAlleleNamesExtractor alleleNamesExtractor,
            IMacDictionary macDictionary,
            IAlleleGroupExpander groupExpander)
        {
            this.hlaCategorisationService = hlaCategorisationService;
            this.alleleNamesExtractor = alleleNamesExtractor;
            this.macDictionary = macDictionary;
            this.groupExpander = groupExpander;
        }

        public async Task<IReadOnlyCollection<string>> ConvertHla(
            Locus locus,
            string hlaName,
            ExpressionSuffixBehaviour behaviour,
            string hlaNomenclatureVersion)
        {
            // A name whose category cannot be determined has no data, and TryConvertHla promises to report that as
            // `false` rather than throw. The categoriser says it with an AtlasHttpException, which would escape as
            // though it were an infrastructure fault.
            var inputCategory = hlaCategorisationService.GetCategoryOrThrowInvalidHla(locus, hlaName);

            switch (inputCategory)
            {
                case HlaTypingCategory.Allele:
                    return GetTwoFieldAlleleNames(locus, new[] { hlaName }, behaviour);
                case HlaTypingCategory.GGroup:
                    var gGroupAlleles = await groupExpander.ExpandAlleleGroup(locus, hlaName, hlaNomenclatureVersion);
                    return GetTwoFieldAlleleNames(locus, gGroupAlleles, behaviour);
                case HlaTypingCategory.PGroup:
                    var pGroupAlleles = await groupExpander.ExpandAlleleGroup(locus, hlaName, hlaNomenclatureVersion);
                    return GetTwoFieldAlleleNames(locus, pGroupAlleles, behaviour);
                case HlaTypingCategory.AlleleStringOfNames:
                case HlaTypingCategory.AlleleStringOfSubtypes:
                    var allelesFromAlleleString = alleleNamesExtractor.GetAlleleNamesFromAlleleString(hlaName);
                    return GetTwoFieldAlleleNames(locus, allelesFromAlleleString, behaviour);
                case HlaTypingCategory.NmdpCode:
                    // See MacLookup.GetAlleleLookupNames: the MAC dictionary reports "not in the store" in its own
                    // vocabulary, and only a genuine storage failure should leave here as itself.
                    try
                    {
                        var allelesForNmdpCode = await macDictionary.GetHlaFromMac(hlaName);
                        return GetTwoFieldAlleleNames(locus, allelesForNmdpCode, behaviour);
                    }
                    catch (Exception e) when (e is MacNotFoundException or ArgumentException)
                    {
                        throw new InvalidHlaException(locus, hlaName);
                    }
                case HlaTypingCategory.XxCode:
                    throw new NotImplementedException("XX Code to Two Field Conversion has not been implemented.");
                case HlaTypingCategory.Serology:
                    throw new NotImplementedException("Serology to Two Field Conversion has not been implemented.");
                default:
                    // A category with no two-field conversion is a name this converter cannot answer for, not an
                    // infrastructure fault. NotImplementedException above is deliberately left alone: those two are
                    // unbuilt features, not missing data.
                    throw new InvalidHlaException(locus, hlaName);
            }
        }

        private static IReadOnlyCollection<string> GetTwoFieldAlleleNames(Locus locus, IEnumerable<string> alleleNames,
            ExpressionSuffixBehaviour behaviour)
        {
            return alleleNames
                .Select(allele => GetTwoFieldAlleleName(locus, allele, behaviour))
                .Distinct()
                .ToList();
        }

        private static string GetTwoFieldAlleleName(Locus locus, string alleleName, ExpressionSuffixBehaviour behaviour)
        {
            var alleleTyping = new AlleleTyping(locus, alleleName);
            return behaviour == ExpressionSuffixBehaviour.Include
                ? alleleTyping.TwoFieldNameIncludingExpressionSuffix
                : alleleTyping.TwoFieldNameExcludingExpressionSuffix;
        }
    }
}