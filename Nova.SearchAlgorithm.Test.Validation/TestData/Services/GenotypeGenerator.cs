using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Builders;
using Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions;
using Nova.SearchAlgorithm.Test.Validation.TestData.Helpers;
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
        private static readonly IAlleleRepository AlleleRepository = new AlleleRepository();
        private static readonly GenotypeCriteria DefaultCriteria = new GenotypeCriteriaBuilder().Build();


        public static Genotype GenerateGenotype(GenotypeCriteria criteria)
        {
            if (criteria == null)
            {
                return RandomGenotype(DefaultCriteria);
            }

            // Naive implementation - if any locus requires p-group match, ensure all loci set up from p-group matching dataset
            // TODO: NOVA-1662: Allow specification per-locus
            if (criteria.PGroupMatchPossible.ToEnumerable().Any(x => x))
            {
                return GenotypeForPGroupMatching();
            }

            // Naive implementation - if any locus requires g-group match, ensure all loci set up from g-group matching dataset
            // TODO: NOVA-1662: Allow specification per-locus
            if (criteria.GGroupMatchPossible.ToEnumerable().Any(x => x))
            {
                return GenotypeForGGroupMatching();
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
                var randomTgsAllele1 = RandomTgsAllele(locus, TypePositions.One, criteria);
                hla.SetAtLocus(locus, TypePositions.One, randomTgsAllele1);

                if (criteria.IsHomozygous.DataAtLocus(locus))
                {
                    hla.SetAtLocus(locus, TypePositions.Two, randomTgsAllele1);
                }
                else
                {
                    var randomTgsAllele2 = RandomTgsAllele(locus, TypePositions.Two, criteria);
                    hla.SetAtLocus(locus, TypePositions.Two, randomTgsAllele2);
                }
            }

            return new Genotype
            {
                Hla = hla
            };
        }

        private static TgsAllele RandomTgsAllele(Locus locus, TypePositions position, GenotypeCriteria criteria)
        {
            var tgsHlaTypingCategory = criteria.TgsHlaCategories.DataAtPosition(locus, position);
            var threeFieldMatchPossible = criteria.ThreeFieldMatchPossible.DataAtPosition(locus, position);
            var twoFieldMatchPossible = criteria.TwoFieldMatchPossible.DataAtPosition(locus, position);
            var alleles = GetDataset(locus, position, tgsHlaTypingCategory, threeFieldMatchPossible, twoFieldMatchPossible);
            return TgsAllele.FromTestDataAllele(alleles.GetRandomElement());
        }

        private static List<AlleleTestData> GetDataset(
            Locus locus,
            TypePositions position,
            TgsHlaTypingCategory tgsHlaTypingCategory,
            bool threeFieldMatchPossible,
            bool twoFieldMatchPossible
        )
        {
            if (threeFieldMatchPossible)
            {
                if (tgsHlaTypingCategory != TgsHlaTypingCategory.FourFieldAllele)
                {
                    throw new InvalidTestDataException("Genotype cannot have a three field match without being explicitly four-field TGS typed");
                }

                return AlleleRepository.DonorAllelesWithThreeFieldMatchPossible().DataAtPosition(locus, position);
            }

            if (twoFieldMatchPossible)
            {
                if (tgsHlaTypingCategory != TgsHlaTypingCategory.ThreeFieldAllele)
                {
                    throw new InvalidTestDataException(
                        "Two field (not third field) match required. But such test data only exists for three-field TGS alleles.");
                }

                return AlleleRepository.AllelesWithTwoFieldMatchPossible().DataAtPosition(locus, position);
            }

            List<AlleleTestData> alleles;
            switch (tgsHlaTypingCategory)
            {
                case TgsHlaTypingCategory.FourFieldAllele:
                    alleles = AlleleRepository
                        .FourFieldAlleles()
                        .DataAtPosition(locus, position);
                    break;
                case TgsHlaTypingCategory.ThreeFieldAllele:
                    alleles = AlleleRepository
                        .ThreeFieldAlleles()
                        .DataAtPosition(locus, position);
                    break;
                case TgsHlaTypingCategory.TwoFieldAllele:
                    alleles = AlleleRepository
                        .TwoFieldAlleles()
                        .DataAtPosition(locus, position);
                    break;
                case TgsHlaTypingCategory.Arbitrary:
                    // Randomly choose dataset here rather than randomly choosing alleles from full dataset,
                    // as otherwise the data is skewed towards the larger dataset (4-field)
                    alleles =
                        new List<List<AlleleTestData>>
                        {
                            AlleleRepository.FourFieldAlleles().DataAtPosition(locus, position),
                            AlleleRepository.ThreeFieldAlleles().DataAtPosition(locus, position),
                            AlleleRepository.TwoFieldAlleles().DataAtPosition(locus, position)
                        }.GetRandomElement();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }

            return alleles;
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
                    var allele1 = alleles.GetRandomElement();
                    var allele2 = alleles.GetRandomElement();

                    return new Tuple<TgsAllele, TgsAllele>(
                        TgsAllele.FromTestDataAllele(allele1),
                        TgsAllele.FromTestDataAllele(allele2)
                    );
                })
            };
        }

        /// <summary>
        /// Creates a full Genotype from the available dataset curated to give g-group level matches.
        /// </summary>
        private static Genotype GenotypeForGGroupMatching()
        {
            return new Genotype
            {
                Hla = AlleleRepository.AllelesForGGroupMatching().Map((l, p, alleles) =>
                {
                    var allele = alleles.GetRandomElement();
                    return TgsAllele.FromTestDataAllele(allele);
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
            Hla = NonMatchingAlleles.Alleles.Map((l, p, a) => TgsAllele.FromTestDataAllele(a))
        };
    }
}