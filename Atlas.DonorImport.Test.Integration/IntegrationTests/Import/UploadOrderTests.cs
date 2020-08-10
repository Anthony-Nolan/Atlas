using System;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using FluentAssertions;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import
{
    [TestFixture]
    public class UploadOrderTests
    {
        private Builder<DonorUpdate> createUpdateBuilder;
        private Builder<DonorUpdate> editUpdateBuilder;
        private Builder<DonorUpdate> deleteUpdateBuilder;
        private Builder<DonorImportFile> fileBuilder;
        private IDonorFileImporter donorFileImporter;
        private IDonorInspectionRepository donorRepository;

        public UploadOrderTests()
        {
            createUpdateBuilder = DonorUpdateBuilder.New.With(update => update.ChangeType, ImportDonorChangeType.Create);
            editUpdateBuilder = DonorUpdateBuilder.New.With(update => update.ChangeType, ImportDonorChangeType.Edit);
            deleteUpdateBuilder = DonorUpdateBuilder.New.With(update => update.ChangeType, ImportDonorChangeType.Delete);
            fileBuilder = DonorImportFileBuilder.NewWithoutContents;
            
            donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
            donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
        }

        [Test]
        public async Task DonorImportOrder_CreateThenCreateImport_ThrowsException()
        {
            var donor1ExternalCode = "1";
            var donor1 = createUpdateBuilder.With(d => d.RecordId, donor1ExternalCode).WithHomozygousHlaAt(Locus.A, "*01:01").Build();
            var create = new[]
            {
                donor1
            };
            
            var createFile1 = fileBuilder.WithDonors(create)
                .With(f => f.FileLocation, "file1")
                .With(f => f.UploadTime, DateTime.Now.AddDays(-1))
                .Build();

            await donorFileImporter.ImportDonorFile(createFile1);
            var result1 = await donorRepository.GetDonor(donor1ExternalCode);
            result1.Should().BeEquivalentTo(donor1);

        }
    }
}