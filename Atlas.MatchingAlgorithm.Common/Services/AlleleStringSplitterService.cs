using System;
using System.Collections.Generic;
using System.Net;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Services.AlleleStringSplitters;
using Nova.Utils.Http.Exceptions;

namespace Atlas.MatchingAlgorithm.Common.Services
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
                    throw new NovaHttpException(
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
                throw new NovaHttpException(
                    HttpStatusCode.BadRequest,
                    "Could not split the submitted allele string.",
                    ex);
            }

            return alleleNames;
        }
    }
}
