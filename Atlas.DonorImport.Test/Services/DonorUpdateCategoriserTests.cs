using System.Linq;
using Atlas.Common.ApplicationInsights;
using Atlas.DonorImport.ApplicationInsights;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services
{
    [TestFixture]
    internal class DonorUpdateCategoriserTests
    {
        private IDonorUpdateCategoriser categoriser;
        private ILogger logger;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<ILogger>();
            categoriser = new DonorUpdateCategoriser(logger);
        }

        [Test]
        public void Categorise_CategorisesDonorUpdatesAsValidOrInvalid()
        {
            var validDonorUpdate = DonorUpdateBuilder.New.Build(1).ToList();
            var invalidDonorUpdate = DonorUpdateBuilder.New.WithHla(null).Build(1).ToList();

            var result = categoriser.Categorise(validDonorUpdate.Concat(invalidDonorUpdate));

            result.ValidDonors.Select(d => d.RecordId).Should().BeEquivalentTo(validDonorUpdate.Select(d => d.RecordId));
            result.InvalidDonors.Select(d => d.RecordId).Should().BeEquivalentTo(invalidDonorUpdate.Select(d => d.RecordId));
        }

        [Test]
        public void Categorise_LogsOneEventForEachUniqueValidationError()
        {
            const int noHlaCount = 3;
            var invalidNoHla = DonorUpdateBuilder.New.WithHla(null).Build(noHlaCount).ToList();

            const int noDrb1Count = 5;
            var hlaMissingDrb1 = HlaBuilder.Default.WithValidHlaAtAllLoci().With(x => x.DRB1, (ImportedLocus)null).Build();
            var invalidMissingDrb1 = DonorUpdateBuilder.New.WithHla(hlaMissingDrb1).Build(noDrb1Count);

            var result = categoriser.Categorise(invalidNoHla.Concat(invalidMissingDrb1));

            result.ValidDonors.Should().BeEmpty();
            result.InvalidDonors.Should().HaveCount(noHlaCount + noDrb1Count);
            logger.Received(2).SendEvent(Arg.Any<SearchableDonorValidationErrorEventModel>());
        }
    }
}
