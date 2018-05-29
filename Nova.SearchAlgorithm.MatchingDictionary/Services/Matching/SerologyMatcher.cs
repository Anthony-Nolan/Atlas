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
            List<IAlleleInfoForMatching> alleleInfo,
            List<RelDnaSer> relDnaSer,
            ISerologyInfoForMatching serologyInfo)
        {
            var matchLocus = serologyInfo.TypingUsedInMatching.MatchLocus;
            var matchingSerologies = serologyInfo.MatchingSerologies.Select(m => m.Name);

            var alleles = (
                from allele in alleleInfo
                join dnaToSer in relDnaSer
                    on new { allele.TypingUsedInMatching.WmdaLocus, allele.TypingUsedInMatching.Name }
                    equals new { dnaToSer.WmdaLocus, dnaToSer.Name }
                where allele.TypingUsedInMatching.MatchLocus.Equals(matchLocus)
                      && dnaToSer.Serologies.Intersect(matchingSerologies).Any()
                select allele
                ).ToList();

            return new MatchedSerology(
                serologyInfo,
                alleles.SelectMany(allele => allele.MatchingPGroups).Distinct(),
                alleles.SelectMany(allele => allele.MatchingGGroups).Distinct());
        }
    }
}
