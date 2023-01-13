using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
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
            var inputCategory = hlaCategorisationService.GetHlaTypingCategory(hlaName);

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
                    var allelesForNmdpCode = await macDictionary.GetHlaFromMac(hlaName);
                    return GetTwoFieldAlleleNames(locus, allelesForNmdpCode, behaviour);
                case HlaTypingCategory.XxCode:
                    throw new NotImplementedException("XX Code to Two Field Conversion has not been implemented.");
                case HlaTypingCategory.Serology:
                    throw new NotImplementedException("Serology to Two Field Conversion has not been implemented.");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static IReadOnlyCollection<string> GetTwoFieldAlleleNames(Locus locus, IEnumerable<string> alleleNames, ExpressionSuffixBehaviour behaviour)
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
