using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Matching
{
    /// <summary>
    /// Creates a complete collection of matched alleles
    /// from the information that was extracted from the WMDA files.
    /// </summary>
    internal class AlleleMatcher : IHlaMatcher
    {
        public IEnumerable<IMatchedHla> CreateMatchedHla(HlaInfoForMatching hlaInfo)
        {
            return hlaInfo.AlleleInfoForMatching.Select(allele =>
                GetMatchedAllele(hlaInfo.SerologyInfoForMatching, hlaInfo.RelDnaSer, allele));
        }

        private static MatchedAllele GetMatchedAllele(
            IList<ISerologyInfoForMatching> serologyToSerology,
            IEnumerable<RelDnaSer> relDnaSer,
            IAlleleInfoForMatching alleleInfoForMatching)
        {
            var allele = (AlleleTyping)alleleInfoForMatching.HlaTyping;
            var molecularLocus = allele.WmdaLocus;
            var usedName = alleleInfoForMatching.TypingUsedInMatching.Name;
            var alleleFamily = ConvertAlleleFamilyToSerology(molecularLocus, usedName);

            var mappingInfo = GetMappingInfoFromRelDnaSer(serologyToSerology, relDnaSer, molecularLocus, usedName, alleleFamily);

            var isAlleleFamilyInvalidSerology =
                !allele.IsDeleted
                && !allele.IsNullExpresser
                && !serologyToSerology.Any(s => s.HlaTyping.Equals(alleleFamily));

            if (isAlleleFamilyInvalidSerology)
                mappingInfo.Add(CreateMappingFromAlleleFamily(alleleFamily));

            return new MatchedAllele(alleleInfoForMatching, mappingInfo);
        }

        private static HlaTyping ConvertAlleleFamilyToSerology(string molecularLocus, string alleleName)
        {
            var serologyLocus = LocusNames.GetSerologyLocusNameFromMolecular(molecularLocus);
            var serologyName = alleleName.Split(':')[0].TrimStart('0');
            return new HlaTyping(serologyLocus, serologyName);
        }

        private static IList<RelDnaSerMapping> GetMappingInfoFromRelDnaSer(
            IList<ISerologyInfoForMatching> serologyToSerology,
            IEnumerable<RelDnaSer> relDnaSer,
            string molecularLocus,
            string usedName,
            HlaTyping alleleFamily)
        {
            var relDnaSerForAllele = relDnaSer.SingleOrDefault(r =>
                r.WmdaLocus.Equals(molecularLocus) && r.Name.Equals(usedName));

            if (relDnaSerForAllele == null || !relDnaSerForAllele.Assignments.Any())
                return new List<RelDnaSerMapping>();

            var expectedMatchingSerology = serologyToSerology
                    .FirstOrDefault(m => m.HlaTyping.Equals(alleleFamily))
                    ?.MatchingSerologies;

            return (
                from serology in serologyToSerology
                join assigned in relDnaSerForAllele.Assignments
                    on serology.TypingUsedInMatching.Name equals assigned.Name
                where serology.TypingUsedInMatching.MatchLocus.Equals(alleleFamily.MatchLocus)
                select new RelDnaSerMapping(
                    (SerologyTyping)serology.HlaTyping,
                    assigned.Assignment,
                    serology.MatchingSerologies.Select(actualMatchingSerology =>
                        GetSerologyMatchInfo(actualMatchingSerology, alleleFamily, expectedMatchingSerology))
                )).ToList();
        }

        private static RelDnaSerMapping CreateMappingFromAlleleFamily(IWmdaHlaTyping alleleFamily)
        {
            var newSerology = new SerologyTyping(alleleFamily, SerologySubtype.NotSplit);
            return new RelDnaSerMapping(
                newSerology,
                Assignment.None,
                new List<RelDnaSerMatch> { new RelDnaSerMatch(newSerology) }
            );
        }

        private static RelDnaSerMatch GetSerologyMatchInfo(
            SerologyTyping actualMatchingSerology,
            HlaTyping alleleFamily,
            IEnumerable<SerologyTyping> expectedMatchingSerology
        )
        {
            var matchInfo = new RelDnaSerMatch(actualMatchingSerology);

            if (actualMatchingSerology.IsDeleted 
                || UnexpectedDnaToSerologyMappings.PermittedExceptions.Contains(alleleFamily))
                return matchInfo;

            matchInfo.IsUnexpected =
                expectedMatchingSerology == null 
                || !expectedMatchingSerology.Contains(actualMatchingSerology);

            return matchInfo;
        }
    }
}
