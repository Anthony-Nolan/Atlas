using System;
using System.Collections.Generic;
using System.Net;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services.AlleleStringSplitters;
using Atlas.Common.Utils.Http;

namespace Atlas.Common.GeneticData.Hla.Services
{
    public interface IAlleleStringSplitterService
    {
        IEnumerable<string> GetAlleleNamesFromAlleleString(string alleleString);
    }

    public class AlleleStringSplitterService : IAlleleStringSplitterService
    {
        private readonly IHlaCategorisationService categorisationService;

        public AlleleStringSplitterService(IHlaCategorisationService categorisationService)
        {
            this.categorisationService = categorisationService;
        }

        public IEnumerable<string> GetAlleleNamesFromAlleleString(string alleleString)
        {
            var typingCategory = categorisationService.GetHlaTypingCategory(alleleString);
            AlleleStringSplitterBase splitter;

            switch (typingCategory)
            {
                case HlaTypingCategory.AlleleStringOfNames:
                    splitter = new AlleleStringOfNamesSplitter();
                    break;
                case HlaTypingCategory.AlleleStringOfSubtypes:
                    splitter = new AlleleStringOfSubtypesSplitter();
                    break;
                default:
                    throw new AtlasHttpException(
                        HttpStatusCode.BadRequest,
                        $"Hla typing is of category {typingCategory}; please submit an allele string.",
                        new ArgumentException());
            }

            IEnumerable<string> alleleNames;
            try
            {
                alleleNames = splitter.GetAlleleNamesFromAlleleString(alleleString);
            }
            catch (Exception ex)
            {
                throw new AtlasHttpException(
                    HttpStatusCode.BadRequest,
                    "Could not split the submitted allele string.",
                    ex);
            }

            return alleleNames;
        }
    }
}
