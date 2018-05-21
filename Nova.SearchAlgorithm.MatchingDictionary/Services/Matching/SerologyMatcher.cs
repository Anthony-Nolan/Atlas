using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Matching
{
    /// <summary>
    /// This class is responsible for 
    /// creating a complete collection of matched serologies
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
            var matchLocus = serologyToMatch.TypeUsedInMatching.MatchLocus;
            var matchingSerologies = serologyToMatch.MatchingSerologies.Select(m => m.Name);

            var matchingPGroups =
                from allele in allelesToPGroups
                join dnaToSer in relDnaSer
                    on new { allele.TypeUsedInMatching.WmdaLocus, allele.TypeUsedInMatching.Name }
                    equals new { dnaToSer.WmdaLocus, dnaToSer.Name }
                where allele.TypeUsedInMatching.MatchLocus.Equals(matchLocus)
                      && dnaToSer.Serologies.Intersect(matchingSerologies).Any()
                select allele.MatchingPGroups;

            return new MatchedSerology(
                serologyToMatch,
                matchingPGroups.SelectMany(m => m).Distinct());
        }
    }
}
