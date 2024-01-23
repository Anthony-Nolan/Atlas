using Atlas.DonorImport.ExternalInterface.Settings;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import
{
    [TestFixture]
    public class FullModeDonorImportTests
    {
        private IDonorFileImporter donorFileImporter;
        private IDonorInspectionRepository donorInspectionRepository;
        private DonorImportSettings settings;


        [SetUp]
        public void SetUp()
        {
            donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
            donorInspectionRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
            settings = DependencyInjection.DependencyInjection.Provider.GetService<DonorImportSettings>();
            DatabaseManager.ClearDatabases();
        }

        [TearDown]
        public void TearDown()
        {
            // Ensure any mocks set up for this test do not stick around.
            DependencyInjection.DependencyInjection.BackingProvider = DependencyInjection.ServiceConfiguration.CreateProvider();
            DatabaseManager.ClearDatabases();
        }

        [Test]
        public async Task ImportDonorFile_WhenUpdateModeIsFull_AndFullModeImportIsntAllowed_DoesNotWriteToDatabase()
        {
            var file = DonorImportFileBuilder.NewWithoutContents.WithDonorCount(10, true);
            settings.AllowFullModeImport = false;   

            await donorFileImporter.ImportDonorFile(file);

            var donorCount = await donorInspectionRepository.DonorCount();
            donorCount.Should().Be(0);
        }

        [Test]
        public async Task ImportDonorFile_WhenUpdateModeIsFull_AndFullModeImportIsAllowed_WriteDonorsToDatabase()
        {
            var file = DonorImportFileBuilder.NewWithoutContents.WithDonorCount(10, true);
            settings.AllowFullModeImport = true;

            await donorFileImporter.ImportDonorFile(file);

            var donorCount = await donorInspectionRepository.DonorCount();
            donorCount.Should().Be(10);
        }

    }
}
