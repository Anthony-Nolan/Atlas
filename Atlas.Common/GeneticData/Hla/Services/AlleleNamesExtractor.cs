using Atlas.Common.GeneticData.Hla.Models;
using System;
using System.Collections.Generic;

namespace Atlas.Common.GeneticData.Hla.Services
{
    public interface IAlleleNamesExtractor
    {
        /// <summary>
        /// Categorises the <paramref name="alleleString"/> and then applies appropriate allele string splitter.
        /// </summary>
        IEnumerable<string> GetAlleleNamesFromAlleleString(string alleleString);
    }

    internal class AlleleNamesExtractor : IAlleleNamesExtractor
    {
        private readonly IHlaCategorisationService categorisationService;

        public AlleleNamesExtractor(IHlaCategorisationService categorisationService)
        {
            this.categorisationService = categorisationService;
        }

        public IEnumerable<string> GetAlleleNamesFromAlleleString(string alleleString)
        {
            var typingCategory = categorisationService.GetHlaTypingCategory(alleleString);

            return typingCategory switch
            {
                HlaTypingCategory.AlleleStringOfNames => AlleleStringSplitter.SplitAlleleStringOfNamesToAlleleNames(alleleString),
                HlaTypingCategory.AlleleStringOfSubtypes => AlleleStringSplitter.SplitAlleleStringOfSubtypesToAlleleNames(alleleString),
                _ => throw new ArgumentOutOfRangeException($"Hla typing is of category {typingCategory}; please submit an allele string.")
            };
        }
    }
}
