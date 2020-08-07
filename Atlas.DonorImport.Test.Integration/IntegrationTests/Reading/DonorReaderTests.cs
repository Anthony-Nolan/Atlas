using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Reading
{
    [TestFixture]
    internal class DonorReaderTests
    {
        private IDonorFileImporter donorFileImporter;
        private IDonorInspectionRepository donorRepository;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
                donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
            });
        }
        
        [TearDown]
        public void OneTimeTearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearDatabases);
        }

        [Test]
        public async Task StreamAllDonors_ReturnsAllDonors()
        {
            const int numberOfDonors = 10;
            var donorFile = DonorImportFileBuilder.NewWithoutContents.WithDonorCount(numberOfDonors).Build();
            await donorFileImporter.ImportDonorFile(donorFile);

            var donors = donorRepository.StreamAllDonors().ToList();

            donors.Count.Should().Be(numberOfDonors);
        }

        [Test]
        public async Task GetDonorsByIds_ReturnsSelectedDonors()
        {
            var donorUpdates = DonorUpdateBuilder.New.Build(10).ToArray();
            var donorFile = DonorImportFileBuilder.NewWithoutContents.WithDonors(donorUpdates).Build();
            await donorFileImporter.ImportDonorFile(donorFile);
            
            var donorCodesOfInterest = donorUpdates.Select(d => d.RecordId).Skip(1).Take(3).ToList();
            var allDonors = donorRepository.StreamAllDonors().ToList();
            var atlasIdsOfInterest = allDonors
                .Where(d => donorCodesOfInterest.Contains(d.ExternalDonorCode))
                .Select(d => d.AtlasId)
                .ToList();
            
            var donors = await donorRepository.GetDonorsByIds(atlasIdsOfInterest);

            donors.Count.Should().Be(atlasIdsOfInterest.Count);
        }

        [Test]
        public async Task GetDonorsByIds_WithMoreDonorsThanSqlParameterisationLimit_ReturnsAllDonors()
        {
            // SQL parameterisation limit is 2100
            const int donorCount = 2200;
            var donorUpdates = DonorUpdateBuilder.New.Build(donorCount).ToArray();
            var donorFile = DonorImportFileBuilder.NewWithoutContents.WithDonors(donorUpdates).Build();
            await donorFileImporter.ImportDonorFile(donorFile);

            var allDonorIds = donorRepository.StreamAllDonors().Select(d => d.AtlasId).ToList();
            
            var donors = await donorRepository.GetDonorsByIds(allDonorIds);

            donors.Count.Should().Be(donorCount);
        }
    }
}