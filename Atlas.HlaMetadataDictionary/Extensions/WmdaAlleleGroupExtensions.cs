using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Extensions
{
    internal static class WmdaAlleleGroupExtensions
    {
        public static IEnumerable<string> GetAlleleNamesWithLocus(this IWmdaAlleleGroup alleleGroup)
        {
            return alleleGroup.Alleles.Select(a => $"{alleleGroup.TypingLocus}*{a}");
        }
    }
}