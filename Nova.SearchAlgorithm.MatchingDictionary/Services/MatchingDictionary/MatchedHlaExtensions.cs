using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary
{
    /// <summary>
    /// Converts matched HLA objects to dictionary entry objects 
    /// so the data is optimally modeled for matching dictionary lookups.
    /// </summary>
    public static class MatchedHlaExtensions
    {
        public static IEnumerable<PreCalculatedHlaMatchInfo> ToPreCalculatedHlaMatchInfo(this IEnumerable<IMatchedHla> matchedHla)
        {
            var matchedHlaList = matchedHla.ToList();

            var entries = new List<PreCalculatedHlaMatchInfo>();
            entries.AddRange(GetPreCalculatedHlaMatchInfoFromSerologies(matchedHlaList.OfType<IMatchingDictionarySource<SerologyTyping>>()));
            entries.AddRange(GetPreCalculatedHlaMatchInfoFromAlleles(matchedHlaList.OfType<IMatchingDictionarySource<AlleleTyping>>()));

            return entries;
        }

        private static IEnumerable<PreCalculatedHlaMatchInfo> GetPreCalculatedHlaMatchInfoFromSerologies(IEnumerable<IMatchingDictionarySource<SerologyTyping>> matchedSerology)
        {
            return matchedSerology.Select(serology => new PreCalculatedHlaMatchInfo(serology));
        }

        private static IEnumerable<PreCalculatedHlaMatchInfo> GetPreCalculatedHlaMatchInfoFromAlleles(IEnumerable<IMatchingDictionarySource<AlleleTyping>> matchedAlleles)
        {
            var entries = matchedAlleles.SelectMany(GetPreCalculatedHlaMatchInfoForEachMolecularSubtype);

            var groupByQueryToMergeDuplicateEntriesCausedByAlleleNameTruncation = entries
                .GroupBy(e => new { e.MatchLocus, e.LookupName })
                .Select(e => new PreCalculatedHlaMatchInfo(
                    e.Key.MatchLocus,
                    e.Key.LookupName,
                    TypingMethod.Molecular,
                    e.Select(m => m.MolecularSubtype).OrderBy(m => m).First(),
                    SerologySubtype.NotSerologyTyping,
                    e.Select(m => m.AlleleTypingStatus).First(),
                    e.SelectMany(p => p.MatchingPGroups).Distinct(),
                    e.SelectMany(g => g.MatchingGGroups).Distinct(),
                    e.SelectMany(s => s.MatchingSerologies).Distinct()
                ));

            return groupByQueryToMergeDuplicateEntriesCausedByAlleleNameTruncation;
        }

        private static IEnumerable<PreCalculatedHlaMatchInfo> GetPreCalculatedHlaMatchInfoForEachMolecularSubtype(
            IMatchingDictionarySource<AlleleTyping> matchedAllele)
        {
            var entries = new List<PreCalculatedHlaMatchInfo>
            {
                GetPreCalculatedHlaMatchInfoFromMatchedAllele(matchedAllele, MolecularSubtype.CompleteAllele),
                GetPreCalculatedHlaMatchInfoFromMatchedAllele(matchedAllele, MolecularSubtype.TwoFieldAllele),
                GetPreCalculatedHlaMatchInfoFromMatchedAllele(matchedAllele, MolecularSubtype.FirstFieldAllele)
            };

            return entries;
        }

        private static PreCalculatedHlaMatchInfo GetPreCalculatedHlaMatchInfoFromMatchedAllele(IMatchingDictionarySource<AlleleTyping> matchedAllele, MolecularSubtype molecularSubtype)
        {
            var lookupName = GetAlleleLookupName(matchedAllele.TypingForMatchingDictionary, molecularSubtype);
            return new PreCalculatedHlaMatchInfo(matchedAllele, lookupName, molecularSubtype);
        }

        private static string GetAlleleLookupName(AlleleTyping alleleTyping, MolecularSubtype molecularSubtype)
        {
            switch (molecularSubtype)
            {
                case MolecularSubtype.CompleteAllele:
                    return alleleTyping.Name;
                case MolecularSubtype.TwoFieldAllele:
                    return alleleTyping.TwoFieldName;
                case MolecularSubtype.FirstFieldAllele:
                    return alleleTyping.Fields.ElementAt(0);
                default:
                    return string.Empty;
            }
        }
    }
}
