using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.Alleles;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources.Alleles.MatchGrades;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Repositories
{
    public interface IAlleleRepository
    {
        PhenotypeInfo<List<AlleleTestData>> FourFieldAlleles();
        PhenotypeInfo<List<AlleleTestData>> ThreeFieldAlleles();
        PhenotypeInfo<List<AlleleTestData>> TwoFieldAlleles();

        /// <returns>
        /// All 2, 3, and 4 field alleles from the datasets generated from TGS-typed donors in Solar.
        /// Does not include manually curated test data used for e.g. p-group/g-group matching
        /// </returns>
        PhenotypeInfo<List<AlleleTestData>> AllTgsAlleles();

        PhenotypeInfo<List<AlleleTestData>> AllelesForGGroupMatching();
        LociInfo<List<AlleleTestData>> DonorAllelesForPGroupMatching();
        LociInfo<AlleleTestData> PatientAllelesForPGroupMatching();
        LociInfo<List<AlleleTestData>> AllelesForCDnaMatching();
        PhenotypeInfo<List<AlleleTestData>> AllelesForProteinMatching();

        PhenotypeInfo<List<AlleleTestData>> DonorAllelesWithThreeFieldMatchPossible();
        PhenotypeInfo<List<AlleleTestData>> PatientAllelesWithThreeFieldMatchPossible();
        PhenotypeInfo<List<AlleleTestData>> AllelesWithTwoFieldMatchPossible();

        PhenotypeInfo<List<AlleleTestData>> AllelesWithAlleleStringOfSubtypesPossible();
        PhenotypeInfo<List<AlleleTestData>> NullAlleles();
        PhenotypeInfo<List<AlleleTestData>> AllelesWithNonNullExpressionSuffix();

        PhenotypeInfo<List<AlleleTestData>> AllelesWithStringsOfSingleAndMultiplePGroupsPossible();
    }

    /// <summary>
    /// Repository layer for accessing test allele data stored in Resources directory.
    /// </summary>
    public class AlleleRepository : IAlleleRepository
    {
        public PhenotypeInfo<List<AlleleTestData>> FourFieldAlleles()
        {
            return Resources.Alleles.TGS.FourFieldAlleles.Alleles;
        }

        public PhenotypeInfo<List<AlleleTestData>> ThreeFieldAlleles()
        {
            return Resources.Alleles.TGS.ThreeFieldAlleles.Alleles;
        }

        public PhenotypeInfo<List<AlleleTestData>> TwoFieldAlleles()
        {
            return Resources.Alleles.TGS.TwoFieldAlleles.Alleles;
        }

        public PhenotypeInfo<List<AlleleTestData>> AllelesForGGroupMatching()
        {
            return GGroupMatchingAlleles.Alleles;
        }

        public LociInfo<List<AlleleTestData>> DonorAllelesForPGroupMatching()
        {
            return PGroupMatchingAlleles.DonorAlleles;
        }

        public LociInfo<AlleleTestData> PatientAllelesForPGroupMatching()
        {
            return PGroupMatchingAlleles.PatientAlleles;
        }

        public LociInfo<List<AlleleTestData>> AllelesForCDnaMatching()
        {
            return CDnaMatchingAlleles.Alleles;
        }

        public PhenotypeInfo<List<AlleleTestData>> AllelesForProteinMatching()
        {
            // 2-field (different 3rd field) dataset curated such that all alleles are full sequences, so they are also valid for protein match grade tests
            return AllelesWithTwoFieldMatchPossible();
        }

        public PhenotypeInfo<List<AlleleTestData>> AllelesWithAlleleStringOfSubtypesPossible()
        {
            return AllTgsAlleles().Map((l, p, alleles) =>
            {
                var allelesGroupedByFirstField = alleles
                    .GroupBy(a => AlleleSplitter.FirstField(a.AlleleName));

                var groupsWithMoreThanOneSecondField = allelesGroupedByFirstField
                    .Where(g => g.ToList().GroupBy(a => AlleleSplitter.SecondField(a.AlleleName)).Count() > 1);

                var validFirstFields = groupsWithMoreThanOneSecondField.Select(x => x).Select(g => g.Key);

                return alleles.Where(a => validFirstFields.Contains(AlleleSplitter.FirstField(a.AlleleName))).ToList();
            });
        }

        public PhenotypeInfo<List<AlleleTestData>> NullAlleles()
        {
            return Resources.Alleles.NullAlleles.Alleles.ToPhenotypeInfo((l, alleles) => alleles);
        }

        public PhenotypeInfo<List<AlleleTestData>> AllelesWithNonNullExpressionSuffix()
        {
            return Resources.Alleles.AllelesWithNonNullExpressionSuffix.Alleles.ToPhenotypeInfo((l, alleles) => alleles);
        }

        public PhenotypeInfo<List<AlleleTestData>> DonorAllelesWithThreeFieldMatchPossible()
        {
            return FourFieldAlleles().Map((locus, position, alleles) =>
            {
                var groupedAlleles = AlleleGroupsWithSharedFirstThreeFields(alleles);

                return alleles.Where(a =>
                {
                    var group = groupedAlleles.SingleOrDefault(g => Equals(g.Key, AlleleSplitter.FirstThreeFieldsAsString(a.AlleleName)));
                    if (group == null)
                    {
                        return false;
                    }

                    // If only two alleles exist in a group with the same first three fields, only allow the first one to be used by donors.
                    // This ensures that patients will always be able to pick the other one, without causing an exact allele level match
                    if (group.Count() == 2)
                    {
                        return group.First().AlleleName == a.AlleleName;
                    }

                    return true;
                }).ToList();
            });
        }

        public PhenotypeInfo<List<AlleleTestData>> PatientAllelesWithThreeFieldMatchPossible()
        {
            return FourFieldAlleles().Map((locus, position, alleles) =>
            {
                var groupedAlleles = AlleleGroupsWithSharedFirstThreeFields(alleles);
                return groupedAlleles.SelectMany(g => g).ToList();
            });
        }

        public PhenotypeInfo<List<AlleleTestData>> AllelesWithTwoFieldMatchPossible()
        {
            return AllelesWithDifferentThirdFields.Alleles.ToPhenotypeInfo((l, alleles) => alleles);
        }

        public PhenotypeInfo<List<AlleleTestData>> AllTgsAlleles()
        {
            return FourFieldAlleles().Map((l, p, alleles) =>
                alleles.Concat(ThreeFieldAlleles().GetPosition(l, p))
                    .Concat(TwoFieldAlleles().GetPosition(l, p))
                    .ToList()
            );
        }

        public PhenotypeInfo<List<AlleleTestData>> AllelesWithStringsOfSingleAndMultiplePGroupsPossible()
        {
            return FourFieldAlleles().Map((l, p, alleles) =>
            {
                var pGroupsWithMultipleAlleles = alleles.GroupBy(a => a.PGroup).Where(g => g.Count() > 1).ToList();
                if (pGroupsWithMultipleAlleles.Count() < 2)
                {
                    throw new InvalidTestDataException("Not enough p-groups with more than one allele in dataset");
                }

                return pGroupsWithMultipleAlleles.SelectMany(a => a).ToList();
            });
        }

        private static IEnumerable<IGrouping<string, AlleleTestData>> AlleleGroupsWithSharedFirstThreeFields(IEnumerable<AlleleTestData> alleles)
        {
            var groupedAlleles = alleles
                .GroupBy(a => AlleleSplitter.FirstThreeFieldsAsString(a.AlleleName))
                .Where(g => g.Count() > 1)
                .ToList();
            return groupedAlleles;
        }
    }
}