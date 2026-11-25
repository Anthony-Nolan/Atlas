using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services
{
    [TestFixture]
    internal class DonorUpdateCategoriserTests
    {
        private IDonorUpdateCategoriser categoriser;
        private IAtlasLogger logger;
        private IDonorReadRepository donorReadRepository;
        private IDonorImportFailureRepository donorImportFailureRepository;
        private Fixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
            logger = Substitute.For<IAtlasLogger>();
            donorReadRepository = Substitute.For<IDonorReadRepository>();
            donorImportFailureRepository = Substitute.For<IDonorImportFailureRepository>();
            categoriser = new DonorUpdateCategoriser(logger, donorReadRepository, donorImportFailureRepository);
        }

        [Test]
        public async Task Categorise_CategorisesDonorUpdatesAsValidOrInvalid()
        {
            var validDonorUpdate = DonorUpdateBuilder.New.Build(1).ToList();
            var invalidDonorUpdate = DonorUpdateBuilder.New.WithHla(null).Build(1).ToList();

            var result = await categoriser.Categorise(validDonorUpdate.Concat(invalidDonorUpdate), string.Empty);

            result.ValidDonors.Select(d => d.RecordId).Should().BeEquivalentTo(validDonorUpdate.Select(d => d.RecordId));
            result.InvalidDonors.Select(d => d.DonorUpdate.RecordId).Should().BeEquivalentTo(invalidDonorUpdate.Select(d => d.RecordId));
        }

        [Test]
        public async Task Categorise_LogsOneEventForEachUniqueValidationError()
        {
            const int noHlaCount = 3;
            var invalidNoHla = DonorUpdateBuilder.New.WithHla(null).Build(noHlaCount).ToList();

            const int noDrb1Count = 5;
            var hlaMissingDrb1 = HlaBuilder.Default.WithValidHlaAtAllLoci().With(x => x.DRB1, (ImportedLocus)null).Build();
            var invalidMissingDrb1 = DonorUpdateBuilder.New.WithHla(hlaMissingDrb1).Build(noDrb1Count);

            var result = await categoriser.Categorise(invalidNoHla.Concat(invalidMissingDrb1), string.Empty);

            result.ValidDonors.Should().BeEmpty();
            result.InvalidDonors.Should().HaveCount(noHlaCount + noDrb1Count);
            logger.Received(2).SendEvent(Arg.Any<string>(), Arg.Any<LogLevel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>());
        }

        [Test]
        public async Task Categorise_ReadsExternalDonorCodesToCreateValidatorContext()
        {
            var donorUpdate = DonorUpdateBuilder.New.With(d => d.ChangeType, ImportDonorChangeType.Edit).Build();

            await categoriser.Categorise(new [] { donorUpdate }, string.Empty);

            await donorReadRepository.Received().GetExistingExternalDonorCodes(Arg.Is<IEnumerable<string>>(l => l.Contains(donorUpdate.RecordId)));
        }

        [Test]
        public async Task Categorise_WhenNoUpdateIsCheckedAgainstDonorStore_DoesNotReadDonorStore()
        {
            var upserts = DonorUpdateBuilder.New.With(d => d.ChangeType, ImportDonorChangeType.Upsert).Build(2);
            var deletions = DonorUpdateBuilder.New.With(d => d.ChangeType, ImportDonorChangeType.Delete).Build(2);

            await categoriser.Categorise(upserts.Concat(deletions).ToList(), fixture.Create<string>());

            await donorReadRepository.DidNotReceiveWithAnyArgs().GetExistingExternalDonorCodes(default);
        }

        [Test]
        public async Task Categorise_ReadsCodesOfEditsAndDifferentialCreationsOnly()
        {
            var creation = DonorUpdateBuilder.New.With(d => d.ChangeType, ImportDonorChangeType.Create).Build();
            var edit = DonorUpdateBuilder.New.With(d => d.ChangeType, ImportDonorChangeType.Edit).Build();
            var upsert = DonorUpdateBuilder.New.With(d => d.ChangeType, ImportDonorChangeType.Upsert).Build();
            var deletion = DonorUpdateBuilder.New.With(d => d.ChangeType, ImportDonorChangeType.Delete).Build();
            List<string> readCodes = null;
            donorReadRepository
                .GetExistingExternalDonorCodes(Arg.Do<IEnumerable<string>>(codes => readCodes = codes.ToList()))
                .Returns(new List<string>());

            await categoriser.Categorise(new[] { creation, edit, upsert, deletion }, fixture.Create<string>());

            readCodes.Should().BeEquivalentTo(creation.RecordId, edit.RecordId);
        }

        [Test]
        public async Task Categorise_ValidatesEditsAgainstStoredDonorCodes()
        {
            var editOfStoredDonor = DonorUpdateBuilder.New.With(d => d.ChangeType, ImportDonorChangeType.Edit).Build();
            var editOfUnknownDonor = DonorUpdateBuilder.New.With(d => d.ChangeType, ImportDonorChangeType.Edit).Build();
            donorReadRepository.GetExistingExternalDonorCodes(default).ReturnsForAnyArgs(new List<string> { editOfStoredDonor.RecordId });

            var result = await categoriser.Categorise(new[] { editOfStoredDonor, editOfUnknownDonor }, fixture.Create<string>());

            result.ValidDonors.Should().ContainSingle().Which.RecordId.Should().Be(editOfStoredDonor.RecordId);
            result.InvalidDonors.Should().ContainSingle().Which.DonorUpdate.RecordId.Should().Be(editOfUnknownDonor.RecordId);
        }


        [Test]
        public async Task Categorise_SavesDonorImportFailureForInvalidDonor()
        {
            const string fileName = "fileName";
            var validDonorUpdate = DonorUpdateBuilder.New.Build(1).ToList();
            var invalidDonorUpdate = DonorUpdateBuilder.New.WithHla(null).Build();

            await categoriser.Categorise(validDonorUpdate.Concat(new[] { invalidDonorUpdate }), fileName);
            
            await donorImportFailureRepository.BulkInsert(
                Arg.Is<IReadOnlyCollection<DonorImportFailure>>(l => l.Any(f => f.ExternalDonorCode == invalidDonorUpdate.RecordId && f.UpdateFile == fileName)));
        }
    }
}
