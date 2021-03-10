using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.DonorImport;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using FluentAssertions;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Import
{
    public class DonorImportTests
    {
        private IDonorImporter donorImporter;
        private IDonorInspectionRepository inspectionRepo;
        private IDonorManagementLogRepository importLogRepository;

        private IDonorReader MockDonorReader { get; set; }

        private Builder<Donor> IncrementingDonorBuilder => DonorBuilder.New.With(d => d.AtlasDonorId, DonorIdGenerator.NextId());

        [SetUp]
        public void SetUp()
        {
            var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IDormantRepositoryFactory>();

            // We want to inspect the dormant database, as this is what the import will have run on
            inspectionRepo = repositoryFactory.GetDonorInspectionRepository();
            importLogRepository = repositoryFactory.GetDonorManagementLogRepository();
            donorImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorImporter>();

            MockDonorReader = DependencyInjection.DependencyInjection.Provider.GetService<IDonorReader>();

            MockDonorReader.StreamAllDonors().Returns(new List<Donor>());
        }

        [Test]
        public async Task DonorImport_AddsNewDonorsToDatabase()
        {
            var donorInfo = IncrementingDonorBuilder.Build();

            MockDonorReader.StreamAllDonors().Returns(new List<Donor> {donorInfo});

            await donorImporter.ImportDonors();
            var donor = await inspectionRepo.GetDonor(donorInfo.AtlasDonorId);

            donor.Should().NotBeNull();
        }

        [TestCase("")]
        [TestCase(null)]
        public async Task DonorImport_WhenDonorHasMissingRequiredHla_DoesNotImportDonor(string missingHla)
        {
            var donorInfo = IncrementingDonorBuilder
                .With(x => x.A_1, missingHla)
                .With(x => x.A_2, missingHla)
                .With(x => x.B_1, missingHla)
                .With(x => x.B_2, missingHla)
                .With(x => x.DRB1_1, missingHla)
                .With(x => x.DRB1_2, missingHla)
                .Build();

            MockDonorReader.StreamAllDonors().Returns(new List<Donor> {donorInfo});

            await donorImporter.ImportDonors();
            var donor = await inspectionRepo.GetDonor(donorInfo.AtlasDonorId);

            donor.Should().BeNull();
        }

        [TestCase("")]
        [TestCase(null)]
        public async Task DonorImport_WhenDonorHasMissingOptionalHla_ImportsDonor(string missingHla)
        {
            var donorInfo = IncrementingDonorBuilder
                .With(x => x.C_1, missingHla)
                .With(x => x.C_2, missingHla)
                .With(x => x.DPB1_1, missingHla)
                .With(x => x.DPB1_2, missingHla)
                .With(x => x.DQB1_1, missingHla)
                .With(x => x.DQB1_2, missingHla)
                .Build();

            MockDonorReader.StreamAllDonors().Returns(new List<Donor> {donorInfo});

            await donorImporter.ImportDonors();
            var donor = await inspectionRepo.GetDonor(donorInfo.AtlasDonorId);

            donor.Should().NotBeNull();
        }
        
        [Test]
        public async Task DonorImport_WhenDonorsShouldBeMarkedAsUpdated_AddsNewDonorLogsToDatabase()
        {
            var donorInfo = IncrementingDonorBuilder.Build();

            MockDonorReader.StreamAllDonors().Returns(new List<Donor> {donorInfo});

            await donorImporter.ImportDonors(true);

            var log = (await importLogRepository.GetDonorManagementLogBatch(new[] {donorInfo.AtlasDonorId})).SingleOrDefault();
            log.Should().NotBeNull();
        }
        
        [Test]
        public async Task DonorImport_WhenDonorsShouldNotBeMarkedAsUpdated_DoesNotAddNewDonorLogsToDatabase()
        {
            var donorInfo = IncrementingDonorBuilder.Build();

            MockDonorReader.StreamAllDonors().Returns(new List<Donor> {donorInfo});

            await donorImporter.ImportDonors(false);

            var log = (await importLogRepository.GetDonorManagementLogBatch(new[] {donorInfo.AtlasDonorId})).SingleOrDefault();
            log.Should().BeNull();
        }
    }
}