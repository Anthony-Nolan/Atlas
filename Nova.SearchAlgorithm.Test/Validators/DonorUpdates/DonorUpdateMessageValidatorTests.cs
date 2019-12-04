using FluentAssertions;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.Validators.DonorUpdates;
using Nova.Utils.ServiceBus.Models;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Validators.DonorUpdates
{
    [TestFixture]
    public class DonorUpdateMessageValidatorTests
    {
        private DonorUpdateMessageValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new DonorUpdateMessageValidator();
        }

        [Test]
        public void Validator_WhenDeserializedBodyIsNull_ShouldHaveValidationError()
        {
            var message = new ServiceBusMessage<SearchableDonorUpdateModel>
            {
                DeserializedBody = null
            };

            var result = validator.Validate(message);

            result.IsValid.Should().BeFalse();
        }
    }
}