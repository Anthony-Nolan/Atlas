using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Configuration;
using Polly;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.DependencyInjection
{
    /// <summary>
    /// Startup wiring for the receiving half of HLA Metadata Dictionary cache invalidation. Called from an app's
    /// <c>ConfigureAppConfiguration</c>, before any trigger binding is resolved.
    /// </summary>
    public static class HlaMetadataDictionaryCacheInvalidationConfiguration
    {
        private const string ConfigurationSection = "HlaMetadataDictionaryNotifications";
        private const string ConnectionStringKey = ConfigurationSection + ":ConnectionString";
        private const string UpdatedTopicKey = ConfigurationSection + ":UpdatedTopic";
        private const string UpdatedSubscriptionKey = ConfigurationSection + ":UpdatedSubscription";
        private const string AutoDeleteOnIdleMinutesKey = ConfigurationSection + ":SubscriptionAutoDeleteOnIdleMinutes";

        private const int DefaultAutoDeleteOnIdleMinutes = 60;

        /// <summary>Service Bus rejects an auto-delete-on-idle window shorter than this.</summary>
        private const int MinimumAutoDeleteOnIdleMinutes = 5;

        /// <summary>Keeps the added start-up delay to at most ~15s on a sustained failure.</summary>
        private const int MaxSubscriptionCreationRetries = 4;

        /// <summary>
        /// Gives this worker instance a subscription of its own on the update topic, creating it if it does not
        /// already exist, and points <c>%HlaMetadataDictionaryNotifications:UpdatedSubscription%</c> at it.
        /// </summary>
        /// <remarks>
        /// A subscription is a competing-consumer queue: every instance of an app sharing one subscription would mean
        /// a single instance receiving each invalidation and the rest continuing to serve their stale copy. Since the
        /// point of the notification is that EVERY process holding a copy drops it, each instance needs a subscription
        /// to itself, and only the instance itself knows which instance it is - so this cannot be declared in
        /// Terraform alongside the topic.
        ///
        /// <para>
        /// Subscriptions are created with an auto-delete-on-idle window, so one belonging to an instance that has
        /// since been scaled away removes itself. A live instance holds an open receiver, which counts as activity
        /// and keeps its own subscription alive.
        /// </para>
        /// </remarks>
        public static IConfigurationBuilder AddPerInstanceHlaMetadataDictionaryCacheInvalidationSubscription(
            this IConfigurationBuilder builder)
        {
            // Environment variables are added explicitly rather than relying on what the builder already holds: the
            // apps start from a bare HostBuilder, so at the point this runs the only sources registered may be ones
            // added before it. Functions app settings - from Azure, or from local.settings.json - arrive as
            // environment variables, which is where these values actually live.
            var configuration = new ConfigurationBuilder()
                .AddConfiguration(builder.Build())
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration[ConnectionStringKey];
            var topic = configuration[UpdatedTopicKey];

            if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(topic))
            {
                throw new InvalidOperationException(
                    $"'{ConnectionStringKey}' and '{UpdatedTopicKey}' must both be configured: without them this app " +
                    "cannot subscribe to HLA Metadata Dictionary updates, and would serve stale HLA metadata for the " +
                    "full cache lifetime after the dictionary is recreated.");
            }

            var subscription = BuildSubscriptionNameForThisInstance();
            var autoDeleteOnIdle = ReadAutoDeleteOnIdle(configuration[AutoDeleteOnIdleMinutesKey]);

            EnsureSubscriptionExists(connectionString, topic, subscription, autoDeleteOnIdle);

            return builder.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>(UpdatedSubscriptionKey, subscription)
            });
        }

        /// <summary>
        /// How long an unused instance subscription may sit before Service Bus deletes it.
        /// </summary>
        /// <remarks>
        /// Validated here rather than left to the <c>[Range]</c> annotation on
        /// <see cref="Settings.HlaMetadataDictionaryNotificationSettings"/>: that annotation only runs when the
        /// settings are bound as options, which happens after the host is built - and the subscribing apps never bind
        /// that type at all, since they reach this configuration through their trigger binding. So a value below the
        /// minimum would reach Service Bus, be rejected, and stop every worker starting with an error that says
        /// nothing about which setting was wrong.
        /// </remarks>
        internal static TimeSpan ReadAutoDeleteOnIdle(string configuredValue)
        {
            // Absent is not a misconfiguration - neither Terraform nor the settings templates set this, and the
            // default is deliberately well clear of the minimum.
            if (string.IsNullOrWhiteSpace(configuredValue))
            {
                return TimeSpan.FromMinutes(DefaultAutoDeleteOnIdleMinutes);
            }

            if (!int.TryParse(configuredValue, out var minutes) || minutes < MinimumAutoDeleteOnIdleMinutes)
            {
                throw new InvalidOperationException(
                    $"'{AutoDeleteOnIdleMinutesKey}' must be a whole number of minutes and no less than " +
                    $"{MinimumAutoDeleteOnIdleMinutes}, which is the shortest auto-delete-on-idle window Service Bus " +
                    $"accepts, but was '{configuredValue}'.");
            }

            return TimeSpan.FromMinutes(minutes);
        }

        /// <remarks>
        /// <c>WEBSITE_INSTANCE_ID</c> is a 64 character hex id and Service Bus caps subscription names at 50, so it is
        /// hashed rather than truncated - the ids of two instances can share a long common prefix. Outside Azure
        /// (local development) there is no such id, so the machine and process stand in, which also lets two locally
        /// running apps hold separate subscriptions.
        /// </remarks>
        internal static string BuildSubscriptionNameForThisInstance()
        {
            var instanceIdentity = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");

            if (string.IsNullOrWhiteSpace(instanceIdentity))
            {
                instanceIdentity = $"{Environment.MachineName}-{Environment.ProcessId}";
            }

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(instanceIdentity))).ToLowerInvariant();

            return $"instance-{hash[..16]}";
        }

        /// <remarks>
        /// Transient faults are retried rather than allowed to stop the app. This runs on the startup path of every
        /// worker instance, including each one added by a scale-out, so without a retry a momentary Service Bus
        /// problem would stop the instance booting - and a scale-out happens precisely when load is high and a missing
        /// instance hurts most. Failing to start is a worse outcome than the staleness this whole mechanism exists to
        /// prevent.
        ///
        /// <para>
        /// Faults a retry cannot fix are deliberately NOT retried and surface immediately: a missing topic or a
        /// connection string without Manage rights is a deploy or configuration error, and should be loud at the first
        /// attempt rather than after a delay. The retry budget is kept short for the same reason - long enough to ride
        /// out a blip, short enough not to run into the platform's own worker start-up timeout.
        /// </para>
        /// </remarks>
        private static void EnsureSubscriptionExists(
            string connectionString,
            string topic,
            string subscription,
            TimeSpan autoDeleteOnIdle)
        {
            var administrationClient = new ServiceBusAdministrationClient(connectionString);

            var retryPolicy = Policy
                .Handle<Exception>(IsWorthRetrying)
                .WaitAndRetry(
                    MaxSubscriptionCreationRetries,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)),
                    // No IAtlasLogger exists this early - the host has not been built - so this goes to stdout, which
                    // the Functions host captures.
                    (exception, _, attempt, _) => Console.WriteLine(
                        $"Could not ensure Service Bus subscription '{subscription}' on topic '{topic}'; " +
                        $"attempt {attempt}/{MaxSubscriptionCreationRetries}; exception: {exception}"));

            try
            {
                retryPolicy.Execute(() =>
                {
                    if (administrationClient.SubscriptionExistsAsync(topic, subscription).GetAwaiter().GetResult())
                    {
                        return;
                    }

                    administrationClient.CreateSubscriptionAsync(new CreateSubscriptionOptions(topic, subscription)
                    {
                        AutoDeleteOnIdle = autoDeleteOnIdle,
                        DeadLetteringOnMessageExpiration = false
                    }).GetAwaiter().GetResult();
                });
            }
            catch (ServiceBusException exception) when (exception.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
            {
                // Two workers on the same instance racing to create the same subscription. Whoever lost has what it
                // wanted regardless.
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Could not ensure this instance's Service Bus subscription '{subscription}' exists on topic " +
                    $"'{topic}', so this app cannot be told when the HLA Metadata Dictionary is recreated and will " +
                    "not start. Check that the topic exists and that " +
                    $"'{ConnectionStringKey}' carries Manage rights on it.",
                    exception);
            }
        }

        /// <summary>
        /// Whether retrying could plausibly succeed. A topic that does not exist, or credentials that do not carry
        /// Manage rights, will still not exist or still lack them a few seconds later - the latter arrives as an
        /// <see cref="UnauthorizedAccessException"/>, which falls to the default below and so is never retried.
        /// </summary>
        internal static bool IsWorthRetrying(Exception exception) => exception switch
        {
            ServiceBusException serviceBusException => serviceBusException.Reason switch
            {
                ServiceBusFailureReason.MessagingEntityAlreadyExists => false,
                ServiceBusFailureReason.MessagingEntityNotFound => false,
                ServiceBusFailureReason.MessagingEntityDisabled => false,
                _ => serviceBusException.IsTransient
            },
            RequestFailedException requestFailed =>
                requestFailed.Status == 0 || requestFailed.Status == 408 || requestFailed.Status == 429 || requestFailed.Status >= 500,
            TimeoutException => true,
            _ => false
        };
    }
}
