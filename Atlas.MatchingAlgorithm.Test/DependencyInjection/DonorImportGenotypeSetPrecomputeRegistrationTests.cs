using System;
using System.Linq;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using GenotypeImputationSettings = Atlas.MatchPrediction.ExternalInterface.Settings.GenotypeImputationSettings;
using HaplotypeFrequencySetCacheSettings = Atlas.MatchPrediction.ExternalInterface.Settings.HaplotypeFrequencySetCacheSettings;

namespace Atlas.MatchingAlgorithm.Test.DependencyInjection
{
    /// <summary>
    /// The Donor Management app's registration: <see cref="ServiceConfiguration.RegisterDonorManagement"/>, then
    /// <see cref="ServiceConfiguration.RegisterDonorImportGenotypeSetPrecompute"/>.
    /// </summary>
    [TestFixture]
    public class DonorImportGenotypeSetPrecomputeRegistrationTests
    {
        /// <summary>Clients are built with it, but never connect: nothing here is called.</summary>
        private const string DevelopmentStorage = "UseDevelopmentStorage=true";

        [Test]
        public void RegisterDonorManagement_Alone_RegistersTheNoOpPrecomputer()
        {
            var services = DonorManagementOnly();

            services.Single(d => d.ServiceType == typeof(IDonorGenotypeSetPrecomputer))
                .ImplementationType.Should().Be(typeof(NoOpDonorGenotypeSetPrecomputer));
        }

        [Test]
        public void RegisterDonorImportGenotypeSetPrecompute_ReplacesTheNoOpPrecomputer()
        {
            var services = DonorManagementOnly();
            RegisterPrecompute(services);

            services.Single(d => d.ServiceType == typeof(IDonorGenotypeSetPrecomputer))
                .ImplementationType.Should().Be(typeof(DonorGenotypeSetPrecomputer));
        }

        // The Donor Management app validates its registrations when it starts in the Development environment
        // (e.g. `func start`). ValidateOnBuild only checks that each service can be built: it creates nothing, so
        // nothing connects to storage.
        [Test]
        public void RegisterDonorImportGenotypeSetPrecompute_AfterRegisterDonorManagement_ValidatesOnBuild()
        {
            var services = DonorManagementOnly();
            RegisterPrecompute(services);

            var buildProvider = () => services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

            buildProvider.Should().NotThrow();
        }

        // Resolving the precomputer builds the HLA Metadata Dictionary, which connects to storage, so these only check
        // that the dependencies are registered. GenotypeSetPipelineRegistrationTests (Match Prediction) validates the
        // pipeline itself.
        [TestCase(typeof(ISubjectGenotypeSetPrecomputeService))]
        [TestCase(typeof(ISubjectGenotypeSetValueService))]
        [TestCase(typeof(Atlas.MatchPrediction.Services.MatchProbability.IGenotypeSetService))]
        [TestCase(typeof(Atlas.MatchPrediction.Services.HaplotypeFrequencies.IHaplotypeFrequencyLookupService))]
        public void RegisterDonorImportGenotypeSetPrecompute_RegistersThePrecomputerDependencies(Type serviceType)
        {
            var services = DonorManagementOnly();
            RegisterPrecompute(services);

            services.Should().Contain(d => d.ServiceType == serviceType);
        }

        private static ServiceCollection DonorManagementOnly()
        {
            var services = new ServiceCollection();
            // Registered by the host itself, as in the Donor Management app's Startup.
            services.AddSingleton(Options.Create(new HaplotypeFrequencySetCacheSettings()));

            services.RegisterDonorManagement(
                _ => new ApplicationInsightsSettings { LogLevel = "Info" },
                _ => new AzureStorageSettings { ConnectionString = DevelopmentStorage },
                _ => new DonorManagementSettings(),
                _ => new HlaMetadataDictionarySettings { AzureStorageConnectionString = DevelopmentStorage },
                _ => new MacDictionarySettings { AzureStorageConnectionString = DevelopmentStorage, TableName = "mac" },
                _ => new MessagingServiceBusSettings(),
                _ => new NotificationsServiceBusSettings(),
                _ => "persistent-sql",
                _ => "transient-a-sql",
                _ => "transient-b-sql",
                _ => "donor-sql");

            return services;
        }

        private static void RegisterPrecompute(IServiceCollection services) =>
            services.RegisterDonorImportGenotypeSetPrecompute(
                _ => new ApplicationInsightsSettings { LogLevel = "Info" },
                _ => new HlaMetadataDictionarySettings { AzureStorageConnectionString = DevelopmentStorage },
                _ => new MacDictionarySettings { AzureStorageConnectionString = DevelopmentStorage, TableName = "mac" },
                _ => new GenotypeImputationSettings(),
                _ => "match-prediction-sql");
    }
}
