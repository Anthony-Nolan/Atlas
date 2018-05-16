using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Matching
{
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
            var allele = (Allele)alleleInfoForMatching.HlaType;
            var molecularLocus = allele.WmdaLocus;
            var usedName = alleleInfoForMatching.TypeUsedInMatching.Name;
            var alleleFamily = ConvertAlleleFamilyToSerology(molecularLocus, usedName);

            var mappingInfo = GetMappingInfoFromRelDnaSer(serologyToSerology, relDnaSer, molecularLocus, usedName, alleleFamily);

            var isAlleleFamilyInvalidSerology =
                !allele.IsDeleted
                && !allele.IsNullExpresser
                && !serologyToSerology.Any(s => s.HlaType.Equals(alleleFamily));

            if (isAlleleFamilyInvalidSerology)
                mappingInfo.Add(CreateMappingFromAlleleFamily(alleleFamily));

            return new MatchedAllele(alleleInfoForMatching, mappingInfo);
        }

        private static HlaType ConvertAlleleFamilyToSerology(string molecularLocus, string alleleName)
        {
            var serologyLocus = LocusNames.GetSerologyLocusNameFromMolecular(molecularLocus);
            var serologyName = alleleName.Split(':')[0].TrimStart('0');
            return new HlaType(serologyLocus, serologyName);
        }

        private static IList<RelDnaSerMapping> GetMappingInfoFromRelDnaSer(
            IList<ISerologyInfoForMatching> serologyToSerology,
            IEnumerable<RelDnaSer> relDnaSer,
            string molecularLocus,
            string usedName,
            HlaType alleleFamily)
        {
            var relDnaSerForAllele = relDnaSer.SingleOrDefault(r =>
                r.WmdaLocus.Equals(molecularLocus) && r.Name.Equals(usedName));

            if (relDnaSerForAllele == null || !relDnaSerForAllele.Assignments.Any())
                return new List<RelDnaSerMapping>();

            var expectedMatchingSerology = serologyToSerology
                    .FirstOrDefault(m => m.HlaType.Equals(alleleFamily))
                    ?.MatchingSerologies;

            return (
                from serology in serologyToSerology
                join assigned in relDnaSerForAllele.Assignments
                    on serology.TypeUsedInMatching.Name equals assigned.Name
                where serology.TypeUsedInMatching.MatchLocus.Equals(alleleFamily.MatchLocus)
                select new RelDnaSerMapping(
                    (Serology)serology.HlaType,
                    assigned.Assignment,
                    serology.MatchingSerologies.Select(actualMatchingSerology =>
                        GetSerologyMatchInfo(actualMatchingSerology, alleleFamily, expectedMatchingSerology))
                )).ToList();
        }

        private static RelDnaSerMapping CreateMappingFromAlleleFamily(IWmdaHlaType alleleFamily)
        {
            var newSerology = new Serology(alleleFamily, SerologySubtype.NotSplit);
            return new RelDnaSerMapping(
                newSerology,
                Assignment.None,
                new List<RelDnaSerMatch> { new RelDnaSerMatch(newSerology) }
            );
        }

        private static RelDnaSerMatch GetSerologyMatchInfo(
            Serology actualMatchingSerology,
            HlaType alleleFamily,
            IEnumerable<Serology> expectedMatchingSerology
        )
        {
            var matchInfo = new RelDnaSerMatch(actualMatchingSerology);

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
