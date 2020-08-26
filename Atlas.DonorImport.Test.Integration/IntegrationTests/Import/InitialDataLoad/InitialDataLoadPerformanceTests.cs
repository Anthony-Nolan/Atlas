using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import.InitialDataLoad
{
    [TestFixture]
    internal class InitialDataLoadPerformanceTests
    {
        private IDonorFileImporter donorFileImporter;

        [SetUp]
        public void SetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
            });
        }

        [TearDown]
        public void TearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearDatabases);
        }

        [Test]
        [IgnoreExceptOnCiPerfTest("Performance Test. 30_000 donors ran in ~17 seconds.")]
        public async Task ImportDonors_AllValid_Performance()
        {
            var file = DonorImportFileBuilder.NewWithoutContents.WithDonorCount(30_000, true);

            await donorFileImporter.ImportDonorFile(file);
        }

        [Test]
        [IgnoreExceptOnCiPerfTest("Performance Test. 100_000 donors ran in ~10 seconds.")]
        public async Task ImportDonors_AllInvalid_Performance()
        {
            var hla = HlaBuilder.New.WithValidHlaAtAllLoci().WithMolecularHlaAtLocus(Locus.B, null, null).Build();
            var donors = DonorUpdateBuilder.New.WithHla(hla).Build(100_000);
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donors.ToArray());

            await donorFileImporter.ImportDonorFile(file);
        }
        
        [Test]
        [IgnoreExceptOnCiPerfTest("Performance Test. 30_000 donors ran in ~11 seconds.")]
        public async Task ImportDonors_HalfInvalid_Performance()
        {
            const int donorCount = 30_000;
            
            var invalidHla = HlaBuilder.New.WithValidHlaAtAllLoci().WithMolecularHlaAtLocus(Locus.B, null, null).Build();
            var invalidDonors = DonorUpdateBuilder.New.WithHla(invalidHla).Build(donorCount/2);
            var validDonors = DonorUpdateBuilder.New.Build(donorCount/2);

            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(invalidDonors.Concat(validDonors).ToArray());
            await donorFileImporter.ImportDonorFile(file);
        }
    }
}