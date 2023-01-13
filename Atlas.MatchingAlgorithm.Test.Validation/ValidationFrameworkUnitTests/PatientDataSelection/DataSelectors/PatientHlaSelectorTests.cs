using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Builders.Criteria;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Repositories;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.DataSelectors;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationFrameworkUnitTests.PatientDataSelection.DataSelectors
{
    [TestFixture]
    public class PatientHlaSelectorTests
    {
        private IPatientHlaSelector patientHlaSelector;
        private IAlleleRepository alleleRepository;

        private readonly LocusPosition[] bothPosition = {LocusPosition.One, LocusPosition.Two};

        private int alleleNumber;
        private PhenotypeInfo<List<AlleleTestData>> alleles;
        private LociInfo<AlleleTestData> patientPGroupAlleles;
        private LociInfo<List<AlleleTestData>> donorPGroupAlleles;
        private PhenotypeInfo<List<AlleleTestData>> gGroupAlleles;

        [SetUp]
        public void SetUp()
        {
            alleleRepository = Substitute.For<IAlleleRepository>();

            alleles = new PhenotypeInfo<bool>().Map((l, p, noop) => new List<AlleleTestData>
            {
                GetTestAllele(l, new[] {p}, MatchLevel.Allele),
                GetTestAllele(l, new[] {p}, MatchLevel.Allele),
            });

            patientPGroupAlleles = new LociInfo<bool>().Map((l, noop) => GetTestAllele(l, bothPosition, MatchLevel.PGroup));

            donorPGroupAlleles = new LociInfo<bool>().Map((l, noop) => new List<AlleleTestData>
            {
                GetTestAllele(l, bothPosition, MatchLevel.PGroup),
                GetTestAllele(l, bothPosition, MatchLevel.PGroup),
            });

            gGroupAlleles = new LociInfo<bool>().Map((l, noop) => new List<AlleleTestData>
            {
                GetTestAllele(l, bothPosition, MatchLevel.GGroup),
                GetTestAllele(l, bothPosition, MatchLevel.GGroup),
            }).ToPhenotypeInfo((l, a) => a);

            alleleRepository.AllelesForGGroupMatching().Returns(gGroupAlleles);
            alleleRepository.PatientAllelesForPGroupMatching().Returns(patientPGroupAlleles);
            alleleRepository.AllTgsAlleles().Returns(alleles);
            alleleRepository.DonorAllelesForPGroupMatching().Returns(donorPGroupAlleles);

            patientHlaSelector = new PatientHlaSelector(alleleRepository);
        }

        [Test]
        public void GetPatientHla_ReturnsMatchingAlleleFromMetaDonorGenotype()
        {
            var criteria = new PatientHlaSelectionCriteriaBuilder().WithMatchOrientationAtLocus(Locus.A, MatchOrientation.Direct).Build();

            var metaDonor = new MetaDonor
            {
                Genotype =
                {
                    Hla = alleles.Map((locus, p, all) => TgsAllele.FromTestDataAllele(all.First()))
                }
            };

            var patientHla = patientHlaSelector.GetPatientHla(metaDonor, criteria);

            patientHla.A.Position1.Should().Be(metaDonor.Genotype.Hla.A.Position1.TgsTypedAllele);
        }

        [Test]
        public void GetPatientHla_ForPGroupMatchLevel_DoesNotSelectExactAlleleMatch()
        {
            var criteria = new PatientHlaSelectionCriteriaBuilder()
                .WithMatchOrientationAtLocus(Locus.A, MatchOrientation.Direct)
                .WithMatchLevelAtAllLoci(MatchLevel.PGroup)
                .Build();

            var metaDonor = new MetaDonor
            {
                Genotype =
                {
                    Hla = donorPGroupAlleles.Map((locus, all) => TgsAllele.FromTestDataAllele(all.First()))
                        .ToPhenotypeInfo((locus, allele) => allele)
                }
            };

            var patientHla = patientHlaSelector.GetPatientHla(metaDonor, criteria);

            patientHla.A.Position1.Should().NotBe(metaDonor.Genotype.Hla.A.Position1.TgsTypedAllele);
        }

        [Test]
        public void GetPatientHla_ForGGroupMatchLevel_DoesNotSelectExactAlleleMatch()
        {
            var criteria = new PatientHlaSelectionCriteriaBuilder()
                .WithMatchOrientationAtLocus(Locus.A, MatchOrientation.Direct)
                .WithMatchLevelAtAllLoci(MatchLevel.GGroup)
                .Build();

            var metaDonor = new MetaDonor
            {
                Genotype =
                {
                    Hla = gGroupAlleles.Map((locus, p, all) => TgsAllele.FromTestDataAllele(all.First()))
                }
            };

            var patientHla = patientHlaSelector.GetPatientHla(metaDonor, criteria);

            patientHla.A.Position1.Should().NotBe(metaDonor.Genotype.Hla.A.Position1.TgsTypedAllele);
        }

        [Test]
        public void GetPatientHla_ForUntypedLocus_ReturnsNull()
        {
            var criteria = new PatientHlaSelectionCriteria
            {
                PatientTypingResolutions = new PhenotypeInfo<HlaTypingResolution>(HlaTypingResolution.Untyped),
                Orientations = new LociInfo<MatchOrientation>(MatchOrientation.Direct)
            };

            var metaDonor = new MetaDonor
            {
                Genotype =
                {
                    Hla = alleles.Map((locus, p, all) => TgsAllele.FromTestDataAllele(all.First()))
                }
            };

            var patientHla = patientHlaSelector.GetPatientHla(metaDonor, criteria);

            patientHla.ToEnumerable().All(x => x == null).Should().BeTrue();
        }

        [TestCase(LocusPosition.One)]
        [TestCase(LocusPosition.Two)]
        public void GetPatientHla_ForHomozygousLocus_WhenShouldMatchAtOnePosition_ReturnsMatchingAlleleAtEachPosition(LocusPosition matchingPosition)
        {
            const Locus locus = Locus.A;
            var criteria = new PatientHlaSelectionCriteriaBuilder()
                .MatchingAtPosition(locus, matchingPosition)
                .NotMatchingAtPosition(locus, matchingPosition.Other())
                .WithMatchOrientationAtLocus(locus, MatchOrientation.Direct)
                .HomozygousAtLocus(locus)
                .Build();

            var metaDonor = new MetaDonor
            {
                Genotype =
                {
                    Hla = alleles.Map((l, p, all) => TgsAllele.FromTestDataAllele(all.First()))
                }
            };

            var patientHla = patientHlaSelector.GetPatientHla(metaDonor, criteria);
            var hlaAtLocus = patientHla.GetLocus(locus);

            hlaAtLocus.Position1.Should().Be(metaDonor.Genotype.Hla.GetPosition(locus, matchingPosition).TgsTypedAllele);
            hlaAtLocus.Position2.Should().Be(metaDonor.Genotype.Hla.GetPosition(locus, matchingPosition).TgsTypedAllele);
        }

        [Test]
        public void GetPatientHla_ForHomozygousLocus_WhenShouldMatchAtNeitherPosition_ReturnsSameAlleleAtEachPosition()
        {
            const Locus locus = Locus.A;
            var criteria = new PatientHlaSelectionCriteriaBuilder()
                .NotMatchingAtEitherPosition(locus)
                .WithMatchOrientationAtLocus(locus, MatchOrientation.Direct)
                .HomozygousAtLocus(locus)
                .Build();

            var metaDonor = new MetaDonor
            {
                Genotype =
                {
                    Hla = alleles.Map((l, p, all) => TgsAllele.FromTestDataAllele(all.First()))
                }
            };

            var patientHla = patientHlaSelector.GetPatientHla(metaDonor, criteria);
            var hlaAtLocus = patientHla.GetLocus(locus);

            hlaAtLocus.Position1.Should().Be(hlaAtLocus.Position2);
        }

        [Test]
        public void GetPatientHla_ForHomozygousLocus_WhenShouldMatchAtBothPositions_AndDonorNotHomozygous_ThrowsException()
        {
            const Locus locus = Locus.A;
            var criteria = new PatientHlaSelectionCriteriaBuilder()
                .MatchingAtBothPositions(locus)
                .WithMatchOrientationAtLocus(locus, MatchOrientation.Direct)
                .HomozygousAtLocus(locus)
                .Build();

            var metaDonor = new MetaDonor
            {
                Genotype =
                {
                    Hla = alleles.Map((l, p, all) => TgsAllele.FromTestDataAllele(all.First()))
                }
            };

            Assert.Throws<HlaSelectionException>(() => patientHlaSelector.GetPatientHla(metaDonor, criteria));
        }

        [Test]
        public void GetPatientHla_ForHomozygousLocus_WhenShouldMatchAtBothPositions_AndDonorHomozygous_ReturnsDonorHla()
        {
            const Locus locus = Locus.A;
            var criteria = new PatientHlaSelectionCriteriaBuilder()
                .MatchingAtBothPositions(locus)
                .WithMatchOrientationAtLocus(locus, MatchOrientation.Direct)
                .HomozygousAtLocus(locus)
                .Build();

            var metaDonor = new MetaDonor
            {
                Genotype =
                {
                    Hla = alleles.MapByLocus((l, hla) =>
                    {
                        var allele = TgsAllele.FromTestDataAllele(hla.Position1.First());
                        return new LocusInfo<TgsAllele>(allele);
                    }),
                }
            };

            var patientHla = patientHlaSelector.GetPatientHla(metaDonor, criteria);
            var hlaAtLocus = patientHla.GetLocus(locus);
            var donorHla1 = metaDonor.Genotype.Hla.GetPosition(locus, LocusPosition.One).TgsTypedAllele;
            var donorHla2 = metaDonor.Genotype.Hla.GetPosition(locus, LocusPosition.Two).TgsTypedAllele;

            hlaAtLocus.Position1.Should().Be(donorHla1);
            hlaAtLocus.Position2.Should().Be(donorHla2);
            donorHla1.Should().Be(donorHla2);
        }

        [Test]
        public void GetPatientHla_ForExpressingMismatch_ReturnsHlaNotMatchingDonor()
        {
            var criteria = new PatientHlaSelectionCriteriaBuilder()
                .WithHlaSourceAtPosition(Locus.A, LocusPosition.One, PatientHlaSource.ExpressingAlleleMismatch)
                .Build();

            var metaDonor = new MetaDonor
            {
                Genotype =
                {
                    Hla = alleles.Map((locus, p, all) => TgsAllele.FromTestDataAllele(all.First()))
                }
            };

            var patientHla = patientHlaSelector.GetPatientHla(metaDonor, criteria);

            patientHla.A.Position1.Should().NotBe(metaDonor.Genotype.Hla.A.Position1.TgsTypedAllele);
        }

        [Test]
        public void GetPatientHla_ForNullMismatch_ReturnsAlleleNotMatchingDonor()
        {
            var criteria = new PatientHlaSelectionCriteriaBuilder()
                .WithHlaSourceAtPosition(Locus.A, LocusPosition.One, PatientHlaSource.NullAlleleMismatch)
                .Build();

            var metaDonor = new MetaDonor
            {
                Genotype =
                {
                    Hla = alleles.Map((locus, p, all) => TgsAllele.FromTestDataAllele(all.First()))
                }
            };

            var patientHla = patientHlaSelector.GetPatientHla(metaDonor, criteria);

            patientHla.A.Position1.Should().NotBe(metaDonor.Genotype.Hla.A.Position1.TgsTypedAllele);
        }

        [Test]
        public void GetPatientHla_ForNullMismatch_ReturnsNullAllele()
        {
            var criteria = new PatientHlaSelectionCriteriaBuilder()
                .WithHlaSourceAtPosition(Locus.A, LocusPosition.One, PatientHlaSource.NullAlleleMismatch)
                .Build();

            var metaDonor = new MetaDonor
            {
                Genotype =
                {
                    Hla = alleles.Map((locus, p, all) => TgsAllele.FromTestDataAllele(all.First()))
                }
            };

            var patientHla = patientHlaSelector.GetPatientHla(metaDonor, criteria);

            patientHla.A.Position1.Should().Contain("N");
        }

        [Test]
        public void GetPatientHla_IncludingNullMismatch_NeverReturnsCrossMatchWhenArbitraryMatchOrientationRequested()
        {
            var criteria = new PatientHlaSelectionCriteriaBuilder()
                .WithMatchOrientationAtLocus(Locus.A, MatchOrientation.Arbitrary)
                .WithHlaSourceAtPosition(Locus.A, LocusPosition.One, PatientHlaSource.NullAlleleMismatch)
                .Build();

            var metaDonor = new MetaDonor
            {
                Genotype =
                {
                    Hla = alleles.Map((locus, p, all) => TgsAllele.FromTestDataAllele(all.First()))
                }
            };

            // There is a random element to the orientation selection
            // To avoid false positives, we repeat this test multiple time to ensure that the result does not change
            for (var i = 0; i < 10; i++)
            {
                var patientHla = patientHlaSelector.GetPatientHla(metaDonor, criteria);

                patientHla.A.Position1.Should().Contain("N");
                patientHla.A.Position2.Should().NotContain("N");
            }
        }

        private AlleleTestData GetTestAllele(Locus locus, LocusPosition[] position, MatchLevel matchLevel)
        {
            string positionString;
            switch (position.Length)
            {
                case 1 when position.Single() == LocusPosition.One:
                    positionString = "1";
                    break;
                case 1 when position.Single() == LocusPosition.Two:
                    positionString = "2";
                    break;
                default:
                    if (position.Contains(LocusPosition.One) && position.Contains(LocusPosition.Two))
                    {
                        positionString = "1&2";
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(position), position, null);
                    }

                    break;
            }

            alleleNumber++;

            return new AlleleTestData
            {
                AlleleName = $"01:01:{alleleNumber}:{locus}-{positionString} matchlevel={matchLevel}",
                PGroup = "p-1",
                GGroup = "g-1",
                NmdpCode = "nmdp-1",
                Serology = "s-1"
            };
        }
    }
}