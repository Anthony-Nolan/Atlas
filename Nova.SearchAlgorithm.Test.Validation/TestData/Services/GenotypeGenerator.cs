using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Builders;
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
        private static readonly GenotypeCriteria DefaultCriteria = new GenotypeCriteriaBuilder().Build();


        public static Genotype GenerateGenotype(GenotypeCriteria criteria)
        {
            if (criteria == null)
            {
                return RandomGenotype(DefaultCriteria);
            }

            // Naive implementation - if any locus requires non-unique p-groups, ensure all loci have non-unique p-groups
            // If we need to specify that some loci have unique p-groups, this will need changing
            if (criteria.HasNonUniquePGroups.ToEnumerable().Any(x => x))
            {
                return GenotypeForPGroupMatching();
            }

            return RandomGenotype(criteria);
        }

        /// <summary>
        /// Creates a random full Genotype from the available TGS allele names
        /// </summary>
        private static Genotype RandomGenotype(GenotypeCriteria criteria)
        {
            var hla = new PhenotypeInfo<TgsAllele>();
            foreach (var locus in LocusHelpers.AllLoci())
            {
                var tgsTypingCategory = criteria.TgsHlaCategories.DataAtLocus(locus);
                hla.SetAtLocus(locus, TypePositions.One, RandomTgsAllele(locus, TypePositions.One, tgsTypingCategory.Item1));
                hla.SetAtLocus(locus, TypePositions.Two, RandomTgsAllele(locus, TypePositions.Two, tgsTypingCategory.Item2));
            }

            return new Genotype
            {
                Hla = hla
            };
        }

        private static TgsAllele RandomTgsAllele(Locus locus, TypePositions position, TgsHlaTypingCategory tgsHlaTypingCategory)
        {
            List<AlleleTestData> alleles;
            switch (tgsHlaTypingCategory)
            {
                case TgsHlaTypingCategory.FourFieldAllele:
                    alleles = AlleleRepository
                        .FourFieldAlleles()
                        .DataAtPosition(locus, position);
                    return TgsAllele.FromFourFieldAllele(GetRandomElement(alleles), locus);
                case TgsHlaTypingCategory.ThreeFieldAllele:
                    alleles = AlleleRepository
                        .ThreeFieldAlleles()
                        .DataAtPosition(locus, position);
                    return TgsAllele.FromThreeFieldAllele(GetRandomElement(alleles), locus);
                case TgsHlaTypingCategory.TwoFieldAllele:
                    alleles = AlleleRepository
                        .TwoFieldAlleles()
                        .DataAtPosition(locus, position);
                    return TgsAllele.FromTwoFieldAllele(GetRandomElement(alleles), locus);
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }
        }

        /// <summary>
        /// Creates a full Genotype from the available dataset curated to give p-group level matches.
        /// The corresponding curated patient hla data must be used to guarantee a p-group level match
        /// </summary>
        private static Genotype GenotypeForPGroupMatching()
        {
            return new Genotype
            {
                Hla = AlleleRepository.DonorAllelesForPGroupMatching().ToPhenotypeInfo((l, alleles) =>
                {
                    var allele1 = GetRandomElement(alleles);
                    var allele2 = GetRandomElement(alleles);

                    return new Tuple<TgsAllele, TgsAllele>(
                        TgsAllele.FromTestDataAllele(allele1, l),
                        TgsAllele.FromTestDataAllele(allele2, l)
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

        private static T GetRandomElement<T>(IReadOnlyList<T> data)
        {
            return data[Random.Next(data.Count)];
        }
    }
}