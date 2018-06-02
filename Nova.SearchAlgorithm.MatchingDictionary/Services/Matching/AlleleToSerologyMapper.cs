using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Matching
{
    internal class AlleleToSerologyMapper
    {
        public IList<SerologyMappingForAllele> GetSerologyMappingsForAllele(IHlaInfoToMapAlleleToSerology hlaInfo, AlleleTyping alleleTyping)
        {
            var alleleFamilyAsTyping = ConvertAlleleFamilyToHlaTypingWithSerologyLocus(alleleTyping);

            var assignments = GetSerologyAssignmentsForAlleleIfExists(hlaInfo.AlleleToSerologyRelationships, alleleTyping);

            var mappingInfo = GetMappingInfoFromSerologyAssignments(
                    hlaInfo.SerologyInfoForMatching,
                    assignments,
                    alleleFamilyAsTyping);

            return mappingInfo;
        }

        private static HlaTyping ConvertAlleleFamilyToHlaTypingWithSerologyLocus(IWmdaHlaTyping allele)
        {
            var serologyLocus = PermittedLocusNames.GetSerologyLocusNameFromMolecularIfExists(allele.WmdaLocus);
            var serologyName = allele.Name.Split(':')[0].TrimStart('0');
            return new HlaTyping(TypingMethod.Serology, serologyLocus, serologyName);
        }

        private static IEnumerable<SerologyAssignment> GetSerologyAssignmentsForAlleleIfExists(
            IEnumerable<RelDnaSer> alleleToSerologyRelationships,
            IWmdaHlaTyping allele)
        {
            var relationshipForAllele = alleleToSerologyRelationships.SingleOrDefault(r =>
                r.WmdaLocus.Equals(allele.WmdaLocus) && r.Name.Equals(allele.Name));

            return relationshipForAllele != null ?
                relationshipForAllele.Assignments : new List<SerologyAssignment>();
        }

        private static IList<SerologyMappingForAllele> GetMappingInfoFromSerologyAssignments(
            IList<ISerologyInfoForMatching> serologiesInfo,
            IEnumerable<SerologyAssignment> assignmentsForAllele,
            HlaTyping alleleFamilyAsTyping)
        {
            var expectedMatchingSerology = serologiesInfo
                    .FirstOrDefault(m => m.HlaTyping.Equals(alleleFamilyAsTyping))
                    ?.MatchingSerologies;

            return (
                from serology in serologiesInfo
                join assigned in assignmentsForAllele
                    on serology.TypingUsedInMatching.Name equals assigned.Name
                where serology.TypingUsedInMatching.MatchLocus.Equals(alleleFamilyAsTyping.MatchLocus)
                select new SerologyMappingForAllele(
                    (SerologyTyping)serology.HlaTyping,
                    assigned.Assignment,
                    serology.MatchingSerologies.Select(actualMatchingSerology =>
                        GetSerologyMatchInfo(actualMatchingSerology, alleleFamilyAsTyping, expectedMatchingSerology))
                )).ToList();
        }

        private static SerologyMatch GetSerologyMatchInfo(
            SerologyTyping actualMatchingSerology,
            HlaTyping alleleFamilyAsTyping,
            IEnumerable<SerologyTyping> expectedMatchingSerology
        )
        {
            var matchInfo = new SerologyMatch(actualMatchingSerology);

            if (actualMatchingSerology.IsDeleted
                || UnexpectedAlleleToSerologyMappings.PermittedExceptions.Contains(alleleFamilyAsTyping))
            {
                return matchInfo;
            }

            matchInfo.IsUnexpected =
                expectedMatchingSerology == null
                || !expectedMatchingSerology.Contains(actualMatchingSerology);

            return matchInfo;
        }
    }
}
