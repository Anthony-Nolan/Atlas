using System;
using System.Linq;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services
{
    /// <summary>
    /// Generates Genotypes from the allele test data
    /// </summary>
    public static class GenotypeGenerator
    {
        private static readonly Random Random = new Random();
        private static readonly IAlleleRepository AlleleRepository = new AlleleRepository();

        public static Genotype GenerateGenotype(GenotypeCriteria criteria)
        {
            if (criteria == null)
            {
                return RandomGenotype();
            }
            
            // Naive implementation - if any locus requires non-unique p-groups, ensure all loci have non-unique p-groups
            // If we need to specify that some loci have unique p-groups, this will need changing
            if (criteria.HasNonUniquePGroups.ToEnumerable().Any(x => x))
            {
                return GenotypeWithNonUniquePGroups();
            }

            return RandomGenotype();
        }

        /// <summary>
        /// Creates a random full Genotype from the available TGS allele names
        /// </summary>
        /// <returns></returns>
        private static Genotype RandomGenotype()
        {
            var tgsFourFieldAlleles = AlleleRepository.FourFieldAlleles().Map((l, p, alleles) =>
                alleles.Select(a => TgsAllele.FromFourFieldAllele(a, l)).ToList());
            
            return new Genotype
            {
                Hla = tgsFourFieldAlleles.Map((locus, position, alleleNames) => alleleNames[Random.Next(alleleNames.Count)])
            };
        }

        /// <summary>
        /// Creates a full Genotype from the available TGS allele names, such that each position's allele corresponds to a p-group that is shared with at least one other allele in the dataset
        /// This is necessary to ensure we can find a match with a lower grade than a full sequence level match
        /// </summary>
        /// <returns></returns>
        private static Genotype GenotypeWithNonUniquePGroups()
        {
            return new Genotype
            {
                Hla = AlleleRepository.FourFieldAllelesWithNonUniquePGroups().MapByLocus((l, alleles1, alleles2) =>
                {
                    var pGroupGroups = alleles1.Concat(alleles2).Distinct().GroupBy(a => a.PGroup).ToList();
                    var selectedPGroup = pGroupGroups[Random.Next(pGroupGroups.Count)];

                    // All p-group level matches will be homozygous.
                    // This cannot be changed util we have enough test data to ensure that the selected patient data will not be a direct or cross match
                    return new Tuple<TgsAllele, TgsAllele>(
                        TgsAllele.FromFourFieldAllele(selectedPGroup.AsEnumerable().First(), l),
                        TgsAllele.FromFourFieldAllele(selectedPGroup.AsEnumerable().First(), l)
                    );
                })
            };
        }

        /// <summary>
        /// A Genotype for which all hla values do not match any others in the repository
        /// </summary>
        ///  TODO: NOVA-1590: Create more robust method of guaranteeing a mismatch
        /// As we're randomly selecting alleles for donors, there's a chance this will actually match
        public static readonly Genotype NonMatchingGenotype = new Genotype
        {
            Hla = NonMatchingAlleles.Alleles.Map((l, p, a) => TgsAllele.FromFourFieldAllele(a, l))
        };
    }
}