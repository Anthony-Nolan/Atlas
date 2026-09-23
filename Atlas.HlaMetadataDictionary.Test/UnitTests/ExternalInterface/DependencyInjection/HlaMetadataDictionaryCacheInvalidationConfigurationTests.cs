using System;
using Azure;
using Azure.Messaging.ServiceBus;
using System.Linq;
using Atlas.HlaMetadataDictionary.ExternalInterface.DependencyInjection;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.ExternalInterface.DependencyInjection
{
    [TestFixture]
    internal class HlaMetadataDictionaryCacheInvalidationConfigurationTests
    {
        private const string InstanceIdVariable = "WEBSITE_INSTANCE_ID";

        /// <summary>A real Azure instance id: 64 hex characters, well over the 50 a subscription name allows.</summary>
        private const string RealisticInstanceId =
            "7c4e1b0a9d8f6c5b4a3928170e6d5c4b3a2918070f6e5d4c3b2a19080706f5e4";

        private string originalInstanceId;

        [SetUp]
        public void SetUp() => originalInstanceId = Environment.GetEnvironmentVariable(InstanceIdVariable);

        [TearDown]
        public void TearDown() => Environment.SetEnvironmentVariable(InstanceIdVariable, originalInstanceId);

        /// <summary>
        /// Service Bus caps subscription names at 50 characters, and WEBSITE_INSTANCE_ID alone is 64.
        /// </summary>
        [Test]
        public void BuildSubscriptionNameForThisInstance_ForAnAzureInstanceId_IsAValidSubscriptionName()
        {
            Environment.SetEnvironmentVariable(InstanceIdVariable, RealisticInstanceId);

            var name = HlaMetadataDictionaryCacheInvalidationConfiguration.BuildSubscriptionNameForThisInstance();

            name.Length.Should().BeLessThanOrEqualTo(50);
            name.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_' or '~').Should().BeTrue();
        }

        [Test]
        public void BuildSubscriptionNameForThisInstance_ForDifferentInstances_DiffersPerInstance()
        {
            Environment.SetEnvironmentVariable(InstanceIdVariable, RealisticInstanceId);
            var first = HlaMetadataDictionaryCacheInvalidationConfiguration.BuildSubscriptionNameForThisInstance();

            // Azure instance ids can share a long common prefix, so the name must not be a simple truncation.
            Environment.SetEnvironmentVariable(InstanceIdVariable, RealisticInstanceId[..60] + "ffff");
            var second = HlaMetadataDictionaryCacheInvalidationConfiguration.BuildSubscriptionNameForThisInstance();

            second.Should().NotBe(first);
        }

        [Test]
        public void BuildSubscriptionNameForThisInstance_ForTheSameInstance_IsStableAcrossRestarts()
        {
            Environment.SetEnvironmentVariable(InstanceIdVariable, RealisticInstanceId);

            HlaMetadataDictionaryCacheInvalidationConfiguration.BuildSubscriptionNameForThisInstance()
                .Should().Be(HlaMetadataDictionaryCacheInvalidationConfiguration.BuildSubscriptionNameForThisInstance());
        }

        /// <summary>
        /// Subscription creation sits on the start-up path of every worker instance, including each one a scale-out
        /// adds. A momentary Service Bus problem must not stop an instance booting - not starting is a worse outcome
        /// than the staleness this mechanism exists to prevent.
        /// </summary>
        [TestCase(ServiceBusFailureReason.ServiceBusy)]
        [TestCase(ServiceBusFailureReason.ServiceCommunicationProblem)]
        [TestCase(ServiceBusFailureReason.ServiceTimeout)]
        public void IsWorthRetrying_ForATransientServiceBusFault_IsTrue(ServiceBusFailureReason reason)
        {
            HlaMetadataDictionaryCacheInvalidationConfiguration
                .IsWorthRetrying(new ServiceBusException("transient", reason))
                .Should().BeTrue();
        }

        /// <summary>
        /// A missing or disabled topic is a deploy error, not a blip: it should surface at the first attempt rather
        /// than after a delay that makes it look like something else.
        /// </summary>
        [TestCase(ServiceBusFailureReason.MessagingEntityNotFound)]
        [TestCase(ServiceBusFailureReason.MessagingEntityDisabled)]
        [TestCase(ServiceBusFailureReason.MessagingEntityAlreadyExists)]
        public void IsWorthRetrying_ForAFaultRetryingCannotFix_IsFalse(ServiceBusFailureReason reason)
        {
            HlaMetadataDictionaryCacheInvalidationConfiguration
                .IsWorthRetrying(new ServiceBusException("permanent", reason))
                .Should().BeFalse();
        }

        /// <summary>A connection string without Manage rights on the topic will not acquire them by waiting.</summary>
        [Test]
        public void IsWorthRetrying_ForAnAuthorisationFailure_IsFalse()
        {
            HlaMetadataDictionaryCacheInvalidationConfiguration
                .IsWorthRetrying(new UnauthorizedAccessException())
                .Should().BeFalse();
        }

        [TestCase(0)]
        [TestCase(408)]
        [TestCase(429)]
        [TestCase(503)]
        public void IsWorthRetrying_ForARetryableHttpStatus_IsTrue(int status)
        {
            HlaMetadataDictionaryCacheInvalidationConfiguration
                .IsWorthRetrying(new RequestFailedException(status, "transient"))
                .Should().BeTrue();
        }

        [TestCase(403)]
        [TestCase(404)]
        public void IsWorthRetrying_ForANonRetryableHttpStatus_IsFalse(int status)
        {
            HlaMetadataDictionaryCacheInvalidationConfiguration
                .IsWorthRetrying(new RequestFailedException(status, "permanent"))
                .Should().BeFalse();
        }

        /// <summary>Outside Azure there is no instance id, but startup still has to produce a usable name.</summary>
        [Test]
        public void BuildSubscriptionNameForThisInstance_WithNoAzureInstanceId_StillBuildsAValidName()
        {
            Environment.SetEnvironmentVariable(InstanceIdVariable, null);

            var name = HlaMetadataDictionaryCacheInvalidationConfiguration.BuildSubscriptionNameForThisInstance();

            name.Should().NotBeNullOrWhiteSpace();
            name.Length.Should().BeLessThanOrEqualTo(50);
        }

        /// <summary>
        /// Service Bus rejects an auto-delete-on-idle window under five minutes, and the [Range] annotation on the
        /// settings class never runs on this path - so an unchecked value would stop every worker starting, with an
        /// error naming neither the setting nor the limit.
        /// </summary>
        [TestCase("4")]
        [TestCase("0")]
        [TestCase("-1")]
        public void ReadAutoDeleteOnIdle_BelowServiceBusMinimum_ThrowsNamingTheSetting(string configured)
        {
            Action read = () => HlaMetadataDictionaryCacheInvalidationConfiguration.ReadAutoDeleteOnIdle(configured);

            read.Should().Throw<InvalidOperationException>()
                .WithMessage("*SubscriptionAutoDeleteOnIdleMinutes*")
                .WithMessage("*5*");
        }

        [TestCase("not-a-number")]
        [TestCase("5.5")]
        public void ReadAutoDeleteOnIdle_NotAWholeNumberOfMinutes_Throws(string configured)
        {
            Action read = () => HlaMetadataDictionaryCacheInvalidationConfiguration.ReadAutoDeleteOnIdle(configured);

            read.Should().Throw<InvalidOperationException>();
        }

        [TestCase("5", 5)]
        [TestCase("60", 60)]
        public void ReadAutoDeleteOnIdle_AtOrAboveTheMinimum_IsUsed(string configured, int expectedMinutes)
        {
            HlaMetadataDictionaryCacheInvalidationConfiguration.ReadAutoDeleteOnIdle(configured)
                .Should().Be(TimeSpan.FromMinutes(expectedMinutes));
        }

        /// <summary>Absent is not a misconfiguration - nothing sets this, and the default clears the minimum.</summary>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ReadAutoDeleteOnIdle_WhenNotConfigured_FallsBackToTheDefault(string configured)
        {
            HlaMetadataDictionaryCacheInvalidationConfiguration.ReadAutoDeleteOnIdle(configured)
                .Should().Be(TimeSpan.FromMinutes(60));
        }
    }
}