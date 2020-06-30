﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;

namespace Atlas.HlaMetadataDictionary.Services.HlaConversion
{
    internal interface IHlaNameToTwoFieldAlleleConverter
    {
        Task<IReadOnlyCollection<string>> ConvertHla(Locus locus, string hlaName, ExpressionSuffixBehaviour behaviour);
    }

    internal enum ExpressionSuffixBehaviour
    {
        Include,
        Exclude
    }

    internal class HlaNameToTwoFieldAlleleConverter : IHlaNameToTwoFieldAlleleConverter
    {
        private readonly IHlaCategorisationService hlaCategorisationService;
        private readonly IAlleleStringSplitterService alleleStringSplitter;
        private readonly IMacDictionary macDictionary;

        public HlaNameToTwoFieldAlleleConverter(
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleStringSplitter,
            IMacDictionary macDictionary)
        {
            this.hlaCategorisationService = hlaCategorisationService;
            this.alleleStringSplitter = alleleStringSplitter;
            this.macDictionary = macDictionary;
        }

        public async Task<IReadOnlyCollection<string>> ConvertHla(Locus locus, string hlaName, ExpressionSuffixBehaviour behaviour)
        {
            var inputCategory = hlaCategorisationService.GetHlaTypingCategory(hlaName);

            switch (inputCategory)
            {
                case HlaTypingCategory.Allele:
                    return new List<string> { GetTwoFieldAlleleName(locus, hlaName, behaviour) };
                case HlaTypingCategory.GGroup:
                    throw new NotImplementedException("GGroup to Two Field Conversion has not been implemented.");
                case HlaTypingCategory.PGroup:
                    throw new NotImplementedException("PGroup to Two Field Conversion has not been implemented.");
                case HlaTypingCategory.AlleleStringOfNames:
                case HlaTypingCategory.AlleleStringOfSubtypes:
                    var allelesFromAlleleString = alleleStringSplitter.GetAlleleNamesFromAlleleString(hlaName);
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
