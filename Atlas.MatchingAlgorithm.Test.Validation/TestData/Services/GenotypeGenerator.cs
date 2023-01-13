using System;
using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Maths;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Builders.Criteria;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Repositories;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Services
{
    /// <summary>
    /// Generates Genotypes from the allele test data
    /// </summary>
    public class GenotypeGenerator
    {
        private readonly IAlleleRepository alleleRepository;
        private static readonly GenotypeCriteria DefaultCriteria = new GenotypeCriteriaBuilder().Build();

        public GenotypeGenerator(IAlleleRepository alleleRepository)
        {
            this.alleleRepository = alleleRepository;
        }

        public Genotype GenerateGenotype(GenotypeCriteria criteria)
        {
            return CreateGenotype(criteria ?? DefaultCriteria);
        }

        /// <summary>
        /// Creates a random full Genotype from the available TGS allele names
        /// </summary>
        private Genotype CreateGenotype(GenotypeCriteria criteria)
        {
            var hla = new PhenotypeInfo<TgsAllele>();
            foreach (var locus in EnumerateValues<Locus>())
            {
                var randomTgsAllele1 = RandomTgsAllele(locus, LocusPosition.One, criteria);
                hla = hla.SetPosition(locus, LocusPosition.One, randomTgsAllele1);

                if (criteria.IsHomozygous.GetLocus(locus))
                {
                    hla = hla.SetPosition(locus, LocusPosition.Two, randomTgsAllele1);
                }
                else
                {
                    var randomTgsAllele2 = RandomTgsAllele(locus, LocusPosition.Two, criteria);
                    hla = hla.SetPosition(locus, LocusPosition.Two, randomTgsAllele2);
                }
            }

            return new Genotype
            {
                Hla = hla
            };
        }

        private TgsAllele RandomTgsAllele(Locus locus, LocusPosition position, GenotypeCriteria criteria)
        {
            var dataset = criteria.AlleleSources.GetPosition(locus, position);

            var alleles = GetDataset(locus, position, dataset);
            if (alleles.IsNullOrEmpty())
            {
                throw new InvalidTestDataException($"No alleles found in dataset: {dataset}");
            }

            var selectedAllele = alleles.GetRandomElement();

            var shouldContainDifferentAlleleGroups = criteria.AlleleStringContainsDifferentAntigenGroups.GetPosition(locus, position);

            return TgsAllele.FromTestDataAllele(
                selectedAllele,
                new AlleleStringOptions
                {
                    NameString = AlleleStringAlleleSelector.GetAllelesForAlleleStringOfNames(dataset, selectedAllele, alleles,
                        shouldContainDifferentAlleleGroups),
                    SubtypeString = AlleleStringAlleleSelector.GetAllelesForAlleleStringOfSubtypes(dataset, selectedAllele, alleles),
                    NameStringWithMultiplePGroups =
                        AlleleStringAlleleSelector.GetAllelesForAlleleStringOfNamesWithMultiplePGroups(selectedAllele, alleles),
                    NameStringWithSinglePGroup = AlleleStringAlleleSelector.GetAllelesForAlleleStringOfNamesWithSinglePGroup(selectedAllele, alleles),
                });
        }

        private List<AlleleTestData> GetDataset(Locus locus, LocusPosition position, Dataset dataset)
        {
            switch (dataset)
            {
                case Dataset.FourFieldTgsAlleles:
                    return alleleRepository.FourFieldAlleles().GetPosition(locus, position);
                case Dataset.ThreeFieldTgsAlleles:
                    return alleleRepository.ThreeFieldAlleles().GetPosition(locus, position);
                case Dataset.TwoFieldTgsAlleles:
                    return alleleRepository.TwoFieldAlleles().GetPosition(locus, position);
                case Dataset.TgsAlleles:
                    // Randomly choose dataset here rather than randomly choosing alleles from full dataset,
                    // as otherwise the data is skewed towards the larger dataset (4-field)
                    return new List<List<AlleleTestData>>
                    {
                        alleleRepository.FourFieldAlleles().GetPosition(locus, position),
                        alleleRepository.ThreeFieldAlleles().GetPosition(locus, position),
                        alleleRepository.TwoFieldAlleles().GetPosition(locus, position)
                    }.GetRandomElement();
                case Dataset.PGroupMatchPossible:
                    return alleleRepository.DonorAllelesForPGroupMatching().GetLocus(locus);
                case Dataset.GGroupMatchPossible:
                    return alleleRepository.AllelesForGGroupMatching().GetPosition(locus, position);
                case Dataset.FourFieldAllelesWithThreeFieldMatchPossible:
                    return alleleRepository.DonorAllelesWithThreeFieldMatchPossible().GetPosition(locus, position);
                case Dataset.ThreeFieldAllelesWithTwoFieldMatchPossible:
                    return alleleRepository.AllelesWithTwoFieldMatchPossible().GetPosition(locus, position);
                case Dataset.AlleleStringOfSubtypesPossible:
                    return alleleRepository.AllelesWithAlleleStringOfSubtypesPossible().GetPosition(locus, position);
                case Dataset.NullAlleles:
                    return alleleRepository.NullAlleles().GetPosition(locus, position);
                case Dataset.AllelesWithNonNullExpressionSuffix:
                    return alleleRepository.AllelesWithNonNullExpressionSuffix().GetPosition(locus, position);
                case Dataset.CDnaMatchPossible:
                    return alleleRepository.AllelesForCDnaMatching().GetLocus(locus);
                case Dataset.ProteinMatchPossible:
                    return alleleRepository.AllelesForProteinMatching().GetPosition(locus, position);
                case Dataset.AllelesWithStringsOfSingleAndMultiplePGroupsPossible:
                    return alleleRepository.AllelesWithStringsOfSingleAndMultiplePGroupsPossible().GetPosition(locus, position);
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataset), dataset, null);
            }
        }
    }
}