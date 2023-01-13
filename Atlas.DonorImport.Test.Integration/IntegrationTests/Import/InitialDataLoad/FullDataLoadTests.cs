using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import.InitialDataLoad
{
    [TestFixture]
    internal class FullDataLoadTests
    {
        private IDonorFileImporter donorFileImporter;
        private IDonorInspectionRepository donorRepository;

        [SetUp]
        public void SetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
                donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
            });
        }

        [TearDown]
        public void TearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearDatabases);
        }

        [Test]
        public async Task FullDonorImport_WhenDonorsAlreadyExist_UpdatesDonors()
        {
            var hla = HlaBuilder.New.WithValidHlaAtAllLoci().Build();
            var donors = DonorUpdateBuilder.New.WithHla(hla).Build(5).ToList();
            var file1 = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donors.ToArray());

            await donorFileImporter.ImportDonorFile(file1);

            var initialDonors = donorRepository.StreamAllDonors().ToList();

            var updatedHla = HlaBuilder.New.WithValidHlaAtAllLoci().WithMolecularHlaAtLocus(Locus.Dqb1, null, null).Build();
            foreach (var donor in donors)
            {
                donor.Hla = updatedHla;
            }

            var file2 = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donors.ToArray());
            await donorFileImporter.ImportDonorFile(file2);

            var updatedDonors = donorRepository.StreamAllDonors().ToList();

            // same donors
            initialDonors.Select(d => d.ExternalDonorCode).Should().BeEquivalentTo(updatedDonors.Select(d => d.ExternalDonorCode));
            // different data
            initialDonors.Select(d => d.Hash).Should().NotBeEquivalentTo(updatedDonors.Select(d => d.Hash));
        }
    }
}