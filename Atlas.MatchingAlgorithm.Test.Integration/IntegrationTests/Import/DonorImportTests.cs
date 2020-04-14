using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nova.DonorService.Client.Models.SearchableDonors;
using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Clients.Http;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Import
{
    public class DonorImportTests
    {
        private IDonorImporter donorImporter;
        private IDonorImportRepository importRepo;
        private IDonorInspectionRepository inspectionRepo;

        private IDonorServiceClient MockDonorServiceClient { get; set; }

        [SetUp]
        public void SetUp()
        {
            var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IDormantRepositoryFactory>();

            importRepo = repositoryFactory.GetDonorImportRepository();
            // We want to inspect the dormant database, as this is what the import will have run on
            inspectionRepo = repositoryFactory.GetDonorInspectionRepository();
            donorImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorImporter>();

            MockDonorServiceClient = DependencyInjection.DependencyInjection.Provider.GetService<IDonorServiceClient>();

            MockDonorServiceClient.GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int?>()).Returns(new SearchableDonorInformationPage
            {
                DonorsInfo = new List<SearchableDonorInformation>()
            });
        }

        [Test]
        public async Task DonorImport_FetchesDonorsWithIdsHigherThanMaxExistingDonor()
        {
            var lowerId = DonorIdGenerator.NextId();
            var higherId = DonorIdGenerator.NextId();
            await importRepo.InsertBatchOfDonors(new List<DonorInfo>
            {
                DonorWithId(higherId),
                DonorWithId(lowerId),
            });

            await donorImporter.ImportDonors();

            await MockDonorServiceClient.Received().GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), higherId);
        }

        [Test]
        public async Task DonorImport_AddsNewDonorsToDatabase()
        {
            var donorInfo = SearchableDonorInformationBuilder.New.Build();

            MockDonorServiceClient.GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int?>()).Returns(new SearchableDonorInformationPage
            {
                DonorsInfo = new List<SearchableDonorInformation> { donorInfo }
            },
                new SearchableDonorInformationPage
                {
                    DonorsInfo = new List<SearchableDonorInformation>()
                });

            await donorImporter.ImportDonors();
            var donor = await inspectionRepo.GetDonor(donorInfo.DonorId);

            donor.Should().NotBeNull();
        }

        [TestCase("Adult", DonorType.Adult)]
        [TestCase("Cord", DonorType.Cord)]
        [TestCase("A", DonorType.Adult)]
        [TestCase("C", DonorType.Cord)]
        public async Task DonorImport_ParsesDonorTypeCorrectly(string rawDonorType, DonorType expectedDonorType)
        {
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.DonorType, rawDonorType)
                .Build();

            MockDonorServiceClient.GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int?>()).Returns(new SearchableDonorInformationPage
            {
                DonorsInfo = new List<SearchableDonorInformation> { donorInfo }
            },
                new SearchableDonorInformationPage
                {
                    DonorsInfo = new List<SearchableDonorInformation>()
                });

            await donorImporter.ImportDonors();
            var donor = await inspectionRepo.GetDonor(donorInfo.DonorId);

            donor.DonorType.Should().Be(expectedDonorType);
        }

        [Test]
        public async Task DonorImport_WhenDonorHasInvalidDonorType_DoesNotImportDonor()
        {
            const string donorType = "invalid";
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.DonorType, donorType)
                .Build();

            MockDonorServiceClient.GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int?>()).Returns(new SearchableDonorInformationPage
            {
                DonorsInfo = new List<SearchableDonorInformation> { donorInfo }
            },
                new SearchableDonorInformationPage
                {
                    DonorsInfo = new List<SearchableDonorInformation>()
                });

            await donorImporter.ImportDonors();
            var donor = await inspectionRepo.GetDonor(donorInfo.DonorId);

            donor.Should().BeNull();
        }

        [Test]
        public void DonorImport_WhenDonorHasInvalidDonorType_DoesNotThrowException()
        {
            const string donorType = "invalid";
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.DonorType, donorType)
                .Build();

            MockDonorServiceClient.GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int?>()).Returns(new SearchableDonorInformationPage
            {
                DonorsInfo = new List<SearchableDonorInformation> { donorInfo }
            },
                new SearchableDonorInformationPage
                {
                    DonorsInfo = new List<SearchableDonorInformation>()
                });

            Assert.DoesNotThrowAsync(async () => await donorImporter.ImportDonors());
        }

        [TestCase("DKMS", RegistryCode.DKMS)]
        [TestCase("AN", RegistryCode.AN)]
        [TestCase("WBS", RegistryCode.WBS)]
        [TestCase("ITALY", RegistryCode.ITALY)]
        [TestCase("NHSBT", RegistryCode.NHSBT)]
        [TestCase("NMDP", RegistryCode.NMDP)]
        public async Task DonorImport_ParsesRegistryCorrectly(string rawRegistry, RegistryCode expectedRegistry)
        {
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.RegistryCode, rawRegistry)
                .Build();

            MockDonorServiceClient.GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int?>()).Returns(new SearchableDonorInformationPage
            {
                DonorsInfo = new List<SearchableDonorInformation> { donorInfo }
            },
                new SearchableDonorInformationPage
                {
                    DonorsInfo = new List<SearchableDonorInformation>()
                });

            await donorImporter.ImportDonors();
            var donor = await inspectionRepo.GetDonor(donorInfo.DonorId);

            donor.RegistryCode.Should().Be(expectedRegistry);
        }

        [Test]
        public async Task DonorImport_WhenDonorHasInvalidRegistryCode_DoesNotImportDonor()
        {
            const string registryCode = "invalid";
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.RegistryCode, registryCode)
                .Build();

            MockDonorServiceClient.GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int?>()).Returns(new SearchableDonorInformationPage
                {
                    DonorsInfo = new List<SearchableDonorInformation> { donorInfo }
                },
                new SearchableDonorInformationPage
                {
                    DonorsInfo = new List<SearchableDonorInformation>()
                });

            await donorImporter.ImportDonors();
            var donor = await inspectionRepo.GetDonor(donorInfo.DonorId);

            donor.Should().BeNull();
        }

        [Test]
        public void DonorImport_WhenDonorHasInvalidRegistryCode_DoesNotThrowException()
        {
            const string registryCode = "invalid";
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.RegistryCode, registryCode)
                .Build();

            MockDonorServiceClient.GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int?>()).Returns(new SearchableDonorInformationPage
            {
                DonorsInfo = new List<SearchableDonorInformation> { donorInfo }
            },
                new SearchableDonorInformationPage
                {
                    DonorsInfo = new List<SearchableDonorInformation>()
                });

            Assert.DoesNotThrowAsync(async () => await donorImporter.ImportDonors());
        }

        [TestCase("")]
        [TestCase(null)]
        public async Task DonorImport_WhenDonorHasMissingRequiredHla_DoesNotImportDonor(string missingHla)
        {
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.A_1, missingHla)
                .With(x => x.A_2, missingHla)
                .With(x => x.B_1, missingHla)
                .With(x => x.B_2, missingHla)
                .With(x => x.DRB1_1, missingHla)
                .With(x => x.DRB1_2, missingHla)
                .Build();

            MockDonorServiceClient.GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int?>()).Returns(new SearchableDonorInformationPage
                {
                    DonorsInfo = new List<SearchableDonorInformation> { donorInfo }
                },
                new SearchableDonorInformationPage
                {
                    DonorsInfo = new List<SearchableDonorInformation>()
                });

            await donorImporter.ImportDonors();
            var donor = await inspectionRepo.GetDonor(donorInfo.DonorId);

            donor.Should().BeNull();
        }

        [TestCase("")]
        [TestCase(null)]
        public void DonorImport_WhenDonorHasMissingRequiredHla_DoesNotThrowException(string missingHla)
        {
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.A_1, missingHla)
                .With(x => x.A_2, missingHla)
                .With(x => x.B_1, missingHla)
                .With(x => x.B_2, missingHla)
                .With(x => x.DRB1_1, missingHla)
                .With(x => x.DRB1_2, missingHla)
                .Build();

            MockDonorServiceClient.GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int?>()).Returns(new SearchableDonorInformationPage
                {
                    DonorsInfo = new List<SearchableDonorInformation> { donorInfo }
                },
                new SearchableDonorInformationPage
                {
                    DonorsInfo = new List<SearchableDonorInformation>()
                });

            Assert.DoesNotThrowAsync(async () => await donorImporter.ImportDonors());
        }

        [TestCase("")]
        [TestCase(null)]
        public async Task DonorImport_WhenDonorHasMissingOptionalHla_ImportsDonor(string missingHla)
        {
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.C_1, missingHla)
                .With(x => x.C_2, missingHla)
                .With(x => x.DPB1_1, missingHla)
                .With(x => x.DPB1_2, missingHla)
                .With(x => x.DQB1_1, missingHla)
                .With(x => x.DQB1_2, missingHla)
                .Build();

            MockDonorServiceClient.GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int?>()).Returns(new SearchableDonorInformationPage
                {
                    DonorsInfo = new List<SearchableDonorInformation> { donorInfo }
                },
                new SearchableDonorInformationPage
                {
                    DonorsInfo = new List<SearchableDonorInformation>()
                });

            await donorImporter.ImportDonors();
            var donor = await inspectionRepo.GetDonor(donorInfo.DonorId);

            donor.Should().NotBeNull();
        }

        private static DonorInfo DonorWithId(int id)
        {
            return new DonorInfo
            {
                RegistryCode = RegistryCode.DKMS,
                DonorType = DonorType.Cord,
                DonorId = id,
                HlaNames = new PhenotypeInfo<string>
                {
                    A = { Position1 = "01:01", Position2 = "30:02:01:01" },
                    B = { Position1 = "07:02", Position2 = "08:01" },
                    Drb1 = { Position1 = "01:11", Position2 = "03:41" },
                }
            };
        }
    }
}