using System.Linq;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Services.Search.Precompute;
using Atlas.MatchPrediction.Services.Precompute;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.DependencyInjection
{
    /// <summary>
    /// <see cref="ServiceConfiguration.RegisterPrecomputedDonorGenotypeSetAccess"/>, as the Atlas.Functions app and the
    /// match prediction Worker call it (ATL-221): it must resolve on its own, with none of the rest of the matching
    /// algorithm registered.
    /// </summary>
    [TestFixture]
    public class PrecomputedDonorGenotypeSetAccessRegistrationTests
    {
        /// <summary>Contexts and repositories are built with it, but never connect: nothing here is called.</summary>
        private const string ConnectionString = "Data Source=(local);Initial Catalog=Atlas;Integrated Security=True;";

        [Test]
        public void RegisterPrecomputedDonorGenotypeSetAccess_OnItsOwn_ResolvesTheRealReaderAndWriter()
        {
            var services = new ServiceCollection();
            Register(services);

            using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
            using var scope = provider.CreateScope();

            scope.ServiceProvider.GetRequiredService<IPrecomputedDonorGenotypeSetReader>().Should().BeOfType<PrecomputedDonorGenotypeSetReader>();
            scope.ServiceProvider.GetRequiredService<IPrecomputedDonorGenotypeSetWriter>().Should().BeOfType<PrecomputedDonorGenotypeSetWriter>();
        }

        [Test]
        public void RegisterPrecomputedDonorGenotypeSetAccess_ReplacesAnEarlierReaderAndWriter()
        {
            var services = new ServiceCollection();
            // As RegisterMatchPredictionAlgorithm leaves them: its no-op defaults, which are internal to match prediction.
            services.AddScoped<IPrecomputedDonorGenotypeSetReader>(_ => null);
            services.AddScoped<IPrecomputedDonorGenotypeSetWriter>(_ => null);

            Register(services);

            services.Single(d => d.ServiceType == typeof(IPrecomputedDonorGenotypeSetReader))
                .ImplementationType.Should().Be(typeof(PrecomputedDonorGenotypeSetReader));
            services.Single(d => d.ServiceType == typeof(IPrecomputedDonorGenotypeSetWriter))
                .ImplementationType.Should().Be(typeof(PrecomputedDonorGenotypeSetWriter));
        }

        private static void Register(IServiceCollection services) =>
            services.RegisterPrecomputedDonorGenotypeSetAccess(_ => ConnectionString, _ => ConnectionString, _ => ConnectionString);
    }
}
