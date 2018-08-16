//using System.Collections.Generic;
//using System.Linq;
//using FluentAssertions;
//using Nova.SearchAlgorithm.Client.Models;
//using Nova.SearchAlgorithm.Common.Models;
//using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
//using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
//using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;
//using Nova.SearchAlgorithm.Test.Validation.TestData.Services;
//using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
//using NSubstitute;
//using NUnit.Framework;
//
//namespace Nova.SearchAlgorithm.Test.Validation.ValidationFrameworkUnitTests
//{
//    [TestFixture]
//    public class PatientDataSelectorTests
//    {
//        private PatientDataSelector patientDataSelector;
//        private List<MetaDonor> metaDonors;
//        private IMetaDonorRepository metaDonorRepository;
//        private IAlleleRepository alleleRepository;
//
//        [SetUp]
//        public void SetUp()
//        {
//            metaDonorRepository = Substitute.For<IMetaDonorRepository>();
//            alleleRepository = Substitute.For<IAlleleRepository>();
//
//            patientDataSelector = new PatientDataSelector(alleleRepository);
//        }
//
//        [Test]
//        public void GetPatientHla_ForPGroupMatchLevel_DoesNotSelectExactAlleleMatch()
//        {
//            const DonorType donorType = DonorType.Adult;
//            const RegistryCode registryCode = RegistryCode.AN;
//
//            var alleles = new List<AlleleTestData>
//            {
//                new AlleleTestData {AlleleName = "01:01:01:a-1", PGroup = "p-1", GGroup = "g-1", NmdpCode = "nmdp-1", Serology = "s-1"},
//                new AlleleTestData {AlleleName = "01:01:01:a-2", PGroup = "p-1", GGroup = "g-1", NmdpCode = "nmdp-1", Serology = "s-1"},
//            };
//
//            metaDonors = new List<MetaDonor>
//            {
//                new MetaDonor
//                {
//                    DonorType = donorType,
//                    Registry = registryCode,
//                    Genotype = {Hla = GenotypeHlaWithAlleleAtAllLoci(alleles.First())},
//                    GenotypeCriteria = new GenotypeCriteria
//                    {
//                        HasNonUniquePGroups = new PhenotypeInfo<bool>
//                        {
//                            A_1 = true,
//                        }
//                    }
//                }
//            };
//            
//            alleleRepository.FourFieldAllelesWithNonUniquePGroups().Returns(AllelesAtAllLoci(alleles));
//            metaDonorRepository.AllMetaDonors().Returns(metaDonors);
//            
//            patientDataSelector.MatchingDonorTypes.Add(donorType);
//            patientDataSelector.MatchingRegistries.Add(registryCode);
//            patientDataSelector.SetAsTenOutOfTenMatch();
//            patientDataSelector.SetAsMatchLevelAtAllLoci(MatchLevel.PGroup);
//
//            var patientHla = patientDataSelector.GetPatientHla();
//
//            patientHla.A_1.Should().NotBe(metaDonors.First().Genotype.Hla.A_1.TgsTypedAllele);
//        }
//        
//        [Test]
//        public void GetPatientHla_ReturnsHlaForDonorAtMatchingRegistry()
//        {
//            const DonorType donorType = DonorType.Adult;
//            const RegistryCode registryCode = RegistryCode.AN;
//            const RegistryCode anotherRegistryCode = RegistryCode.DKMS;
//
//            var alleles = new List<AlleleTestData>
//            {
//                new AlleleTestData {AlleleName = "01:01:01:a-1", PGroup = "p-1", GGroup = "g-1", NmdpCode = "nmdp-1", Serology = "s-1"},
//                new AlleleTestData {AlleleName = "01:01:01:a-2", PGroup = "p-1", GGroup = "g-1", NmdpCode = "nmdp-1", Serology = "s-1"},
//            };
//
//            const int nonMatchingDonorIndex = 0;
//            const int matchingDonorIndex = 1;
//            
//            metaDonors = new List<MetaDonor>
//            {
//                new MetaDonor
//                {
//                    DonorType = donorType,
//                    Registry = anotherRegistryCode,
//                    Genotype = {Hla = GenotypeHlaWithAlleleAtAllLoci(alleles[nonMatchingDonorIndex])},
//                    GenotypeCriteria = new GenotypeCriteria(),
//                },
//                new MetaDonor
//                {
//                    DonorType = donorType,
//                    Registry = registryCode,
//                    Genotype = {Hla = GenotypeHlaWithAlleleAtAllLoci(alleles[matchingDonorIndex])},
//                    GenotypeCriteria = new GenotypeCriteria(),
//                }
//            };
//            
//            alleleRepository.FourFieldAlleles().Returns(AllelesAtAllLoci(alleles));
//            metaDonorRepository.AllMetaDonors().Returns(metaDonors);
//            
//            patientDataSelector.MatchingDonorTypes.Add(donorType);
//            patientDataSelector.MatchingRegistries.Add(registryCode);
//            patientDataSelector.SetAsTenOutOfTenMatch();
//
//            var patientHla = patientDataSelector.GetPatientHla();
//
//            patientHla.A_1.Should().NotBe(metaDonors[nonMatchingDonorIndex].Genotype.Hla.A_1.TgsTypedAllele);
//            patientHla.A_1.Should().Be(metaDonors[matchingDonorIndex].Genotype.Hla.A_1.TgsTypedAllele);
//        }        
//        
//        [Test]
//        public void GetPatientHla_ReturnsHlaForDonorOfMatchingType()
//        {
//            const DonorType donorType = DonorType.Adult;
//            const DonorType anotherDonorType = DonorType.Cord;
//            const RegistryCode registryCode = RegistryCode.AN;
//
//            var alleles = new List<AlleleTestData>
//            {
//                new AlleleTestData {AlleleName = "01:01:01:a-1", PGroup = "p-1", GGroup = "g-1", NmdpCode = "nmdp-1", Serology = "s-1"},
//                new AlleleTestData {AlleleName = "01:01:01:a-2", PGroup = "p-1", GGroup = "g-1", NmdpCode = "nmdp-1", Serology = "s-1"},
//            };
//
//            const int nonMatchingDonorIndex = 0;
//            const int matchingDonorIndex = 1;
//            
//            metaDonors = new List<MetaDonor>
//            {
//                new MetaDonor
//                {
//                    DonorType = anotherDonorType,
//                    Registry = registryCode,
//                    Genotype = {Hla = GenotypeHlaWithAlleleAtAllLoci(alleles[nonMatchingDonorIndex])},
//                    GenotypeCriteria = new GenotypeCriteria(),
//                },
//                new MetaDonor
//                {
//                    DonorType = donorType,
//                    Registry = registryCode,
//                    Genotype = {Hla = GenotypeHlaWithAlleleAtAllLoci(alleles[matchingDonorIndex])},
//                    GenotypeCriteria = new GenotypeCriteria(),
//                }
//            };
//            
//            alleleRepository.FourFieldAlleles().Returns(AllelesAtAllLoci(alleles));
//            metaDonorRepository.AllMetaDonors().Returns(metaDonors);
//            
//            patientDataSelector.MatchingDonorTypes.Add(donorType);
//            patientDataSelector.MatchingRegistries.Add(registryCode);
//            patientDataSelector.SetAsTenOutOfTenMatch();
//
//            var patientHla = patientDataSelector.GetPatientHla();
//
//            patientHla.A_1.Should().NotBe(metaDonors[nonMatchingDonorIndex].Genotype.Hla.A_1.TgsTypedAllele);
//            patientHla.A_1.Should().Be(metaDonors[matchingDonorIndex].Genotype.Hla.A_1.TgsTypedAllele);
//        }
//        
//        private static PhenotypeInfo<List<AlleleTestData>> AllelesAtAllLoci(List<AlleleTestData> alleles)
//        {
//            return new PhenotypeInfo<List<AlleleTestData>>
//            {
//                A_1 = alleles,
//                A_2 = alleles,
//                B_1 = alleles,
//                B_2 = alleles,
//                C_1 = alleles,
//                C_2 = alleles,
//                DPB1_1 = alleles,
//                DPB1_2 = alleles,
//                DQB1_1 = alleles,
//                DQB1_2 = alleles,
//                DRB1_1 = alleles,
//                DRB1_2 = alleles,
//            };
//        }
//        
//        private static PhenotypeInfo<TgsAllele> GenotypeHlaWithAlleleAtAllLoci(AlleleTestData allele)
//        {
//            return new PhenotypeInfo<TgsAllele>
//            {
//                A_1 = TgsAllele.FromFourFieldAllele(allele, Locus.A),
//                A_2 = TgsAllele.FromFourFieldAllele(allele, Locus.A),
//                B_1 = TgsAllele.FromFourFieldAllele(allele, Locus.B),
//                B_2 = TgsAllele.FromFourFieldAllele(allele, Locus.B),
//                C_1 = TgsAllele.FromFourFieldAllele(allele, Locus.C),
//                C_2 = TgsAllele.FromFourFieldAllele(allele, Locus.C),
//                DPB1_1 = TgsAllele.FromFourFieldAllele(allele, Locus.Dpb1),
//                DPB1_2 = TgsAllele.FromFourFieldAllele(allele, Locus.Dpb1),
//                DQB1_1 = TgsAllele.FromFourFieldAllele(allele, Locus.Dqb1),
//                DQB1_2 = TgsAllele.FromFourFieldAllele(allele, Locus.Dqb1),
//                DRB1_1 = TgsAllele.FromFourFieldAllele(allele, Locus.Drb1),
//                DRB1_2 = TgsAllele.FromFourFieldAllele(allele, Locus.Drb1),
//            };
//        }
//    }
//}