using Atlas.Common.Public.Models.ServiceBus;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Validators.DonorInfo;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validators.DonorUpdates;

[TestFixture]
public class DonorUpdateMessageValidatorTests
{
    private DonorUpdateMessageValidator validator;

    [SetUp]
    public void SetUp()
    {
        validator = new DonorUpdateMessageValidator();
    }

    [TestCase("")]
    [TestCase(null)]
    public void Validator_WhenLockTokenIsMissing_ShouldHaveValidationError(string missingString)
    {
        var message = new DeserializedMessage<SearchableDonorUpdate> { LockToken = missingString };
        validator.TestValidate(message).ShouldHaveValidationErrorFor(x => x.LockToken);
    }

    [Test]
    public void Validator_WhenDeserializedBodyIsNull_ShouldHaveValidationError()
    {
        var message = new DeserializedMessage<SearchableDonorUpdate> { DeserializedBody = null };
        validator.TestValidate(message).ShouldHaveValidationErrorFor(x => x.DeserializedBody);
    }
}