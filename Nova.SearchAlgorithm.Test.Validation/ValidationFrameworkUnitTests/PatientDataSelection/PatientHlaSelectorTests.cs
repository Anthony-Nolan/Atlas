using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationFrameworkUnitTests.PatientDataSelection
{
    [TestFixture]
    public class PatientHlaSelectorTests
    {
        private IPatientHlaSelector patientHlaSelector;
        private IAlleleRepository alleleRepository;
        private List<AlleleTestData> alleles;

        [SetUp]
        public void SetUp()
        {
            alleles = new List<AlleleTestData>
            {
                new AlleleTestData {AlleleName = "01:01:01:a-1", PGroup = "p-1", GGroup = "g-1", NmdpCode = "nmdp-1", Serology = "s-1"},
                new AlleleTestData {AlleleName = "01:01:01:a-2", PGroup = "p-1", GGroup = "g-1", NmdpCode = "nmdp-1", Serology = "s-1"},
            };

            alleleRepository = Substitute.For<IAlleleRepository>();
            
            alleleRepository.FourFieldAllelesWithNonUniquePGroups().Returns(AllelesAtAllLoci(alleles));

            patientHlaSelector = new PatientHlaSelector(alleleRepository);
        }

        [Test]
        public void GetPatientHla_ForPGroupMatchLevel_DoesNotSelectExactAlleleMatch()
        {
            var criteria = new PatientHlaSelectionCriteria
            {
                MatchLevels = new PhenotypeInfo<int>().Map((l, p, noop) => MatchLevel.PGroup)
            };

            var metaDonor = new MetaDonor
            {
                Genotype =
                {
                    Hla = GenotypeHlaWithAlleleAtAllLoci(alleles.First())
                }
            };
            
            var patientHla = patientHlaSelector.GetPatientHla(metaDonor, criteria);

            patientHla.A_1.Should().NotBe(metaDonor.Genotype.Hla.A_1.TgsTypedAllele);
        }
        
        private static PhenotypeInfo<List<AlleleTestData>> AllelesAtAllLoci(List<AlleleTestData> alleles)
        {
            return new PhenotypeInfo<List<AlleleTestData>>
            {
                A_1 = alleles,
                A_2 = alleles,
                B_1 = alleles,
                B_2 = alleles,
                C_1 = alleles,
                C_2 = alleles,
                DPB1_1 = alleles,
                DPB1_2 = alleles,
                DQB1_1 = alleles,
                DQB1_2 = alleles,
                DRB1_1 = alleles,
                DRB1_2 = alleles,
            };
        }
        
        private static PhenotypeInfo<TgsAllele> GenotypeHlaWithAlleleAtAllLoci(AlleleTestData allele)
        {
            return new PhenotypeInfo<TgsAllele>
            {
                A_1 = TgsAllele.FromFourFieldAllele(allele, Locus.A),
                A_2 = TgsAllele.FromFourFieldAllele(allele, Locus.A),
                B_1 = TgsAllele.FromFourFieldAllele(allele, Locus.B),
                B_2 = TgsAllele.FromFourFieldAllele(allele, Locus.B),
                C_1 = TgsAllele.FromFourFieldAllele(allele, Locus.C),
                C_2 = TgsAllele.FromFourFieldAllele(allele, Locus.C),
                DPB1_1 = TgsAllele.FromFourFieldAllele(allele, Locus.Dpb1),
                DPB1_2 = TgsAllele.FromFourFieldAllele(allele, Locus.Dpb1),
                DQB1_1 = TgsAllele.FromFourFieldAllele(allele, Locus.Dqb1),
                DQB1_2 = TgsAllele.FromFourFieldAllele(allele, Locus.Dqb1),
                DRB1_1 = TgsAllele.FromFourFieldAllele(allele, Locus.Drb1),
                DRB1_2 = TgsAllele.FromFourFieldAllele(allele, Locus.Drb1),
            };
        }
    }
}