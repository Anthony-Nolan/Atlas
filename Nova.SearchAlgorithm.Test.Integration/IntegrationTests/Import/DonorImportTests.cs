using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nova.DonorService.Client.Models.SearchableDonors;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Clients.Http;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Repositories.DonorRetrieval;
using Nova.SearchAlgorithm.Data.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Nova.SearchAlgorithm.Services.DataRefresh;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Import
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
            await importRepo.InsertBatchOfDonors(new List<InputDonor>
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

        [TestCase("a", DonorType.Adult)]
        [TestCase("adult", DonorType.Adult)]
        [TestCase("c", DonorType.Cord)]
        [TestCase("cord", DonorType.Cord)]
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
        public void DonorImport_WhenDonorHasUnrecognisedDonorType_ThrowsException()
        {
            const string unexpectedDonorType = "fossil";
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.DonorType, unexpectedDonorType)
                .Build();

            MockDonorServiceClient.GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int?>()).Returns(new SearchableDonorInformationPage
            {
                DonorsInfo = new List<SearchableDonorInformation> { donorInfo }
            },
                new SearchableDonorInformationPage
                {
                    DonorsInfo = new List<SearchableDonorInformation>()
                });

            Assert.ThrowsAsync<DonorImportHttpException>(() => donorImporter.ImportDonors());
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
        public void DonorImport_WhenDonorHasUnrecognisedRegistryCode_ThrowsException()
        {
            const string unexpectedRegistryCode = "MARS";
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.RegistryCode, unexpectedRegistryCode)
                .Build();

            MockDonorServiceClient.GetDonorsInfoForSearchAlgorithm(Arg.Any<int>(), Arg.Any<int?>()).Returns(new SearchableDonorInformationPage
            {
                DonorsInfo = new List<SearchableDonorInformation> { donorInfo }
            },
                new SearchableDonorInformationPage
                {
                    DonorsInfo = new List<SearchableDonorInformation>()
                });

            Assert.ThrowsAsync<DonorImportHttpException>(() => donorImporter.ImportDonors());
        }

        private static InputDonor DonorWithId(int id)
        {
            return new InputDonor
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