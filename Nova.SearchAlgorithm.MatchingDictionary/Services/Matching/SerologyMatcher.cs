using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Matching
{
    /// <summary>
    /// Creates a complete collection of matched serologies
    /// from the information that was extracted from the WMDA files.
    /// </summary>
    internal class SerologyMatcher : IHlaMatcher
    {
        public IEnumerable<IMatchedHla> CreateMatchedHla(HlaInfoForMatching hlaInfo)
        {
            return hlaInfo.SerologyInfoForMatching.Select(serology =>
                GetMatchedSerology(hlaInfo.AlleleInfoForMatching, hlaInfo.RelDnaSer, serology));
        }

        private static MatchedSerology GetMatchedSerology(
            List<IAlleleInfoForMatching> allelesToPGroups,
            List<RelDnaSer> relDnaSer,
            ISerologyInfoForMatching serologyToMatch)
        {
            var matchLocus = serologyToMatch.TypingUsedInMatching.MatchLocus;
            var matchingSerologies = serologyToMatch.MatchingSerologies.Select(m => m.Name);

            var matchingPGroups =
                from allele in allelesToPGroups
                join dnaToSer in relDnaSer
                    on new { allele.TypingUsedInMatching.WmdaLocus, allele.TypingUsedInMatching.Name }
                    equals new { dnaToSer.WmdaLocus, dnaToSer.Name }
                where allele.TypingUsedInMatching.MatchLocus.Equals(matchLocus)
                      && dnaToSer.Serologies.Intersect(matchingSerologies).Any()
                select allele.MatchingPGroups;

            return new MatchedSerology(
                serologyToMatch,
                matchingPGroups.SelectMany(m => m).Distinct());
        }
    }
}
