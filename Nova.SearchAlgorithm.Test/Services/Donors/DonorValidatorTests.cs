using FluentAssertions;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Services.Donors;
using Nova.Utils.Notifications;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Threading.Tasks;
using ILogger = Nova.Utils.ApplicationInsights.ILogger;

namespace Nova.SearchAlgorithm.Test.Services.Donors
{
    [TestFixture]
    public class DonorValidatorTests
    {
        private IDonorValidator donorValidator;
        private ILogger logger;
        private INotificationsClient notificationsClient;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<ILogger>();
            notificationsClient = Substitute.For<INotificationsClient>();
            donorValidator = new DonorValidator(logger, notificationsClient);
        }

        [Test]
        public async Task ValidateDonorsAsync_ValidDonor_ReturnsDonor()
        {
            const int donorId = 123;

            var result = await donorValidator.ValidateDonorsAsync(new List<InputDonor>
            {
                new InputDonor
                {
                    DonorId = donorId,
                    HlaNames = new Utils.PhenotypeInfo.PhenotypeInfo<string>("hla")
                }
            });

            result.Should().OnlyContain(d => d.DonorId == donorId);
        }

        [Test]
        public void ValidateDonorsAsync_InvalidDonor_DoesNotThrowException()
        {
            Assert.DoesNotThrowAsync(async () =>
            {
                await donorValidator.ValidateDonorsAsync(new List<InputDonor> { new InputDonor() });
            });
        }
    }
}
