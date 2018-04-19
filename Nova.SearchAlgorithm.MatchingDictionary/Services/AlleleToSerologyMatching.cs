using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public class AlleleToSerologyMatching
    {
        public IEnumerable<MatchedAllele> MatchAllelesToSerology(
            IEnumerable<IMatchingPGroups> allelesToPGroups,
            IEnumerable<IMatchingSerology> serologyToSerology,
            IEnumerable<RelDnaSer> relDnaSer)
        {
            return allelesToPGroups.Select(allele =>
                GetMatchedAllele(serologyToSerology.ToList(), relDnaSer, allele));
        }

        private static MatchedAllele GetMatchedAllele(
            IList<IMatchingSerology> serologyToSerology,
            IEnumerable<RelDnaSer> relDnaSer,
            IMatchingPGroups alleleToPGroup)
        {
            var allele = (Allele)alleleToPGroup.HlaType;
            var molecularLocus = allele.WmdaLocus;
            var usedName = alleleToPGroup.TypeUsedInMatching.Name;
            var alleleFamily = ConvertAlleleFamilyToSerology(molecularLocus, usedName);

            var mappingInfo = GetMappingInfoFromRelDnaSer(serologyToSerology, relDnaSer, molecularLocus, usedName, alleleFamily);

            var isAlleleFamilyInvalidSerology =
                !allele.IsDeleted
                && !allele.IsNullExpresser
                && !serologyToSerology.Any(s => s.HlaType.Equals(alleleFamily));

            if (isAlleleFamilyInvalidSerology)
                mappingInfo.Add(CreateMappingFromAlleleFamily(alleleFamily));

            return new MatchedAllele(alleleToPGroup, mappingInfo);
        }

        private static HlaType ConvertAlleleFamilyToSerology(string molecularLocus, string alleleName)
        {
            var serologyLocus = LocusNames.GetSerologyLocusNameFromMolecular(molecularLocus);
            var serologyName = alleleName.Split(':')[0].TrimStart('0');
            return new HlaType(serologyLocus, serologyName);
        }

        private static IList<SerologyMappingInfo> GetMappingInfoFromRelDnaSer(
            IList<IMatchingSerology> serologyToSerology,
            IEnumerable<RelDnaSer> relDnaSer,
            string molecularLocus,
            string usedName,
            HlaType alleleFamily)
        {
            var relDnaSerForAllele = relDnaSer.SingleOrDefault(r =>
                r.WmdaLocus.Equals(molecularLocus) && r.Name.Equals(usedName));

            if (relDnaSerForAllele == null || !relDnaSerForAllele.Assignments.Any())
                return new List<SerologyMappingInfo>();

            var expectedMatchingSerology = serologyToSerology
                    .FirstOrDefault(m => m.HlaType.Equals(alleleFamily))
                    ?.MatchingSerologies;

            return (
                from serology in serologyToSerology
                join assigned in relDnaSerForAllele.Assignments
                    on serology.TypeUsedInMatching.Name equals assigned.Name
                where serology.TypeUsedInMatching.MatchLocus.Equals(alleleFamily.MatchLocus)
                select new SerologyMappingInfo(
                    (Serology)serology.HlaType,
                    assigned.Assignment,
                    serology.MatchingSerologies.Select(actualMatchingSerology =>
                        GetSerologyMatchInfo(actualMatchingSerology, alleleFamily, expectedMatchingSerology))
                )).ToList();
        }

        private static SerologyMappingInfo CreateMappingFromAlleleFamily(IWmdaHlaType alleleFamily)
        {
            var newSerology = new Serology(alleleFamily, Subtype.NotSplit);
            return new SerologyMappingInfo(
                newSerology,
                Assignment.None,
                new List<SerologyMatchInfo> { new SerologyMatchInfo(newSerology) }
            );
        }

        private static SerologyMatchInfo GetSerologyMatchInfo(
            Serology actualMatchingSerology,
            HlaType alleleFamily,
            IEnumerable<Serology> expectedMatchingSerology
        )
        {
            var matchInfo = new SerologyMatchInfo(actualMatchingSerology);

            if (actualMatchingSerology.IsDeleted 
                || UnexpectedMappings.AcceptableSerologies.Contains(alleleFamily))
                return matchInfo;

            matchInfo.IsUnexpected =
                expectedMatchingSerology == null 
                || !expectedMatchingSerology.Contains(actualMatchingSerology);

            return matchInfo;
        }
    }
}
