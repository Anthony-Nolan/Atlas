using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Builders.Criteria;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Helpers;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Repositories;
using Atlas.Utils.Core.Common;
using System;
using System.Collections.Generic;

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
            foreach (var locus in LocusSettings.AllLoci)
            {
                var randomTgsAllele1 = RandomTgsAllele(locus, TypePosition.One, criteria);
                hla.SetAtPosition(locus, TypePosition.One, randomTgsAllele1);

                if (criteria.IsHomozygous.DataAtLocus(locus))
                {
                    hla.SetAtPosition(locus, TypePosition.Two, randomTgsAllele1);
                }
                else
                {
                    var randomTgsAllele2 = RandomTgsAllele(locus, TypePosition.Two, criteria);
                    hla.SetAtPosition(locus, TypePosition.Two, randomTgsAllele2);
                }
            }

            return new Genotype
            {
                Hla = hla
            };
        }

        private TgsAllele RandomTgsAllele(Locus locus, TypePosition position, GenotypeCriteria criteria)
        {
            var dataset = criteria.AlleleSources.DataAtPosition(locus, position);

            var alleles = GetDataset(locus, position, dataset);
            if (alleles.IsNullOrEmpty())
            {
                throw new InvalidTestDataException($"No alleles found in dataset: {dataset}");
            }

            var selectedAllele = alleles.GetRandomElement();

            var shouldContainDifferentAlleleGroups = criteria.AlleleStringContainsDifferentAntigenGroups.DataAtPosition(locus, position);

            return TgsAllele.FromTestDataAllele(
                selectedAllele,
                new AlleleStringOptions
                {
                    NameString = AlleleStringAlleleSelector.GetAllelesForAlleleStringOfNames(dataset, selectedAllele, alleles, shouldContainDifferentAlleleGroups),
                    SubtypeString = AlleleStringAlleleSelector.GetAllelesForAlleleStringOfSubtypes(dataset, selectedAllele, alleles),
                    NameStringWithMultiplePGroups = AlleleStringAlleleSelector.GetAllelesForAlleleStringOfNamesWithMultiplePGroups(selectedAllele, alleles),
                    NameStringWithSinglePGroup = AlleleStringAlleleSelector.GetAllelesForAlleleStringOfNamesWithSinglePGroup(selectedAllele, alleles),
                });
        }

        private List<AlleleTestData> GetDataset(Locus locus, TypePosition position, Dataset dataset)
        {
            switch (dataset)
            {
                case Dataset.FourFieldTgsAlleles:
                    return alleleRepository.FourFieldAlleles().DataAtPosition(locus, position);
                case Dataset.ThreeFieldTgsAlleles:
                    return alleleRepository.ThreeFieldAlleles().DataAtPosition(locus, position);
                case Dataset.TwoFieldTgsAlleles:
                    return alleleRepository.TwoFieldAlleles().DataAtPosition(locus, position);
                case Dataset.TgsAlleles:
                    // Randomly choose dataset here rather than randomly choosing alleles from full dataset,
                    // as otherwise the data is skewed towards the larger dataset (4-field)
                    return new List<List<AlleleTestData>>
                    {
                        alleleRepository.FourFieldAlleles().DataAtPosition(locus, position),
                        alleleRepository.ThreeFieldAlleles().DataAtPosition(locus, position),
                        alleleRepository.TwoFieldAlleles().DataAtPosition(locus, position)
                    }.GetRandomElement();
                case Dataset.PGroupMatchPossible:
                    return alleleRepository.DonorAllelesForPGroupMatching().DataAtLocus(locus);
                case Dataset.GGroupMatchPossible:
                    return alleleRepository.AllelesForGGroupMatching().DataAtPosition(locus, position);
                case Dataset.FourFieldAllelesWithThreeFieldMatchPossible:
                    return alleleRepository.DonorAllelesWithThreeFieldMatchPossible().DataAtPosition(locus, position);
                case Dataset.ThreeFieldAllelesWithTwoFieldMatchPossible:
                    return alleleRepository.AllelesWithTwoFieldMatchPossible().DataAtPosition(locus, position);
                case Dataset.AlleleStringOfSubtypesPossible:
                    return alleleRepository.AllelesWithAlleleStringOfSubtypesPossible().DataAtPosition(locus, position);
                case Dataset.NullAlleles:
                    return alleleRepository.NullAlleles().DataAtPosition(locus, position);
                case Dataset.AllelesWithNonNullExpressionSuffix:
                    return alleleRepository.AllelesWithNonNullExpressionSuffix().DataAtPosition(locus, position);
                case Dataset.CDnaMatchPossible:
                    return alleleRepository.AllelesForCDnaMatching().DataAtLocus(locus);
                case Dataset.ProteinMatchPossible:
                    return alleleRepository.AllelesForProteinMatching().DataAtPosition(locus, position);
                case Dataset.AllelesWithStringsOfSingleAndMultiplePGroupsPossible:
                    return alleleRepository.AllelesWithStringsOfSingleAndMultiplePGroupsPossible().DataAtPosition(locus, position);
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataset), dataset, null);
            }
        }
    }
}