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
        [IgnoreExceptOnCiPerfTest("Performance Test. 10_000 donors ran in ~10 seconds.")]
        public async Task ImportDonors_AllValid_Performance()
        {
            var file = DonorImportFileBuilder.NewWithoutContents.WithDonorCount(10_000);

            await donorFileImporter.ImportDonorFile(file);
        }

        [Test]
        [IgnoreExceptOnCiPerfTest("Performance Test. 100_000 donors ran in ~10 seconds.")]
        public async Task ImportDonors_AllInvalid_Performance()
        {
            var hla = HlaBuilder.New.WithValidHlaAtAllLoci().WithMolecularHlaAtLocus(Locus.B, null, null).Build();
            var donors = DonorUpdateBuilder.New.WithHla(hla).Build(100_000);
            var file = DonorImportFileBuilder.NewWithoutContents.WithDonors(donors.ToArray());

            await donorFileImporter.ImportDonorFile(file);
        }
        
        [Test]
        [IgnoreExceptOnCiPerfTest("Performance Test. 10_000 donors ran in ~5 seconds.")]
        public async Task ImportDonors_HalfInvalid_Performance()
        {
            const int donorCount = 10_000;
            
            var invalidHla = HlaBuilder.New.WithValidHlaAtAllLoci().WithMolecularHlaAtLocus(Locus.B, null, null).Build();
            var invalidDonors = DonorUpdateBuilder.New.WithHla(invalidHla).Build(donorCount/2);
            var validDonors = DonorUpdateBuilder.New.Build(donorCount/2);

            var file = DonorImportFileBuilder.NewWithoutContents.WithDonors(invalidDonors.Concat(validDonors).ToArray());
            await donorFileImporter.ImportDonorFile(file);
        }
    }
}