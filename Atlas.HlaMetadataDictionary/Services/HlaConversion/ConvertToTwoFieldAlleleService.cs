using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.MultipleAlleleCodeDictionary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Services.HlaConversion
{
    internal enum ExpressionSuffixOptions
    {
        Include,
        Exclude
    }

    internal interface IConvertHlaToTwoFieldAlleleService
    {
        Task<IReadOnlyCollection<string>> ConvertHla(Locus locus, string hlaName, ExpressionSuffixOptions option);
    }

    internal class ConvertHlaToTwoFieldAlleleService : IConvertHlaToTwoFieldAlleleService
    {
        private readonly IHlaCategorisationService hlaCategorisationService;
        private readonly IAlleleStringSplitterService alleleStringSplitter;
        private readonly INmdpCodeCache nmdpCodeCache;

        public ConvertHlaToTwoFieldAlleleService(
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleStringSplitter, INmdpCodeCache nmdpCodeCache)
        {
            this.hlaCategorisationService = hlaCategorisationService;
            this.alleleStringSplitter = alleleStringSplitter;
            this.nmdpCodeCache = nmdpCodeCache;
        }

        public async Task<IReadOnlyCollection<string>> ConvertHla(Locus locus, string hlaName, ExpressionSuffixOptions option)
        {
            var category = hlaCategorisationService.GetHlaTypingCategory(hlaName);

            switch (category)
            {
                case HlaTypingCategory.Allele:
                    return new List<string> { GetTwoFieldAlleleName(locus, hlaName, option) };
                case HlaTypingCategory.GGroup:
                    // TODO: ATLAS-370
                    throw new NotImplementedException();
                case HlaTypingCategory.PGroup:
                    // TODO: ATLAS-369
                    throw new NotImplementedException();
                case HlaTypingCategory.AlleleStringOfNames:
                case HlaTypingCategory.AlleleStringOfSubtypes:
                    var allelesFromAlleleString = alleleStringSplitter.GetAlleleNamesFromAlleleString(hlaName);
                    return GetTwoFieldAlleleNames(locus, allelesFromAlleleString, option);
                case HlaTypingCategory.NmdpCode:
                    var allelesForNmdpCode = await nmdpCodeCache.GetOrAddAllelesForNmdpCode(locus, hlaName);
                    return GetTwoFieldAlleleNames(locus, allelesForNmdpCode, option);
                case HlaTypingCategory.XxCode:
                    // TODO: ATLAS-367
                    throw new NotImplementedException();
                case HlaTypingCategory.Serology:
                    // TODO: ATLAS-368
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static IReadOnlyCollection<string> GetTwoFieldAlleleNames(Locus locus, IEnumerable<string> alleleNames, ExpressionSuffixOptions option)
        {
            return alleleNames
                .Select(allele => GetTwoFieldAlleleName(locus, allele, option))
                .Distinct()
                .ToList();
        }

        private static string GetTwoFieldAlleleName(Locus locus, string alleleName, ExpressionSuffixOptions option)
        {
            var alleleTyping = new AlleleTyping(locus, alleleName);
            return option == ExpressionSuffixOptions.Include
                ? alleleTyping.TwoFieldNameIncludingExpressionSuffix
                : alleleTyping.TwoFieldNameExcludingExpressionSuffix;
        }
    }
}
