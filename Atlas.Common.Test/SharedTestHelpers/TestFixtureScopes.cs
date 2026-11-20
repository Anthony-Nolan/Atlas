using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Atlas.Common.Test.SharedTestHelpers
{
    /// <summary>
    /// The dependency injection of an integration test project whose fixtures run at the same time: each fixture has its own DI scope,
    /// so that fixtures do not share scoped services (such as a DbContext, or a connection string read when a service is made),
    /// and a fixture can set a service provider of its own (e.g. with some services substituted) without changing that of the others.
    /// Code that runs outside any fixture, such as a set-up fixture, has a scope of its own and sets the provider of every other fixture.
    /// </summary>
    public static class TestFixtureScopes
    {
        private static IServiceProvider sharedProvider;
        private static readonly ConcurrentDictionary<string, IServiceProvider> ProvidersOfFixtures = new();
        private static readonly ConcurrentDictionary<string, AsyncServiceScope> Scopes = new();

        /// <summary>
        /// The service provider of the current fixture: the one it set, or else the one set outside any fixture.
        /// Setting it starts a new scope for the current fixture.
        /// </summary>
        public static IServiceProvider BackingProvider
        {
            get
            {
                if (ProvidersOfFixtures.TryGetValue(CurrentFixtureId(), out var provider))
                {
                    return provider;
                }

                return sharedProvider ?? throw new InvalidOperationException("The service provider has not been set up");
            }
            set
            {
                if (CurrentFixture() is TestFixture fixture)
                {
                    ProvidersOfFixtures[fixture.Id] = value;
                }
                else
                {
                    sharedProvider = value;
                }

                NewScope();
            }
        }

        /// <summary>
        /// The scope of the current fixture, which it shares with no other fixture.
        /// </summary>
        public static IServiceProvider Provider => Scopes.GetOrAdd(CurrentFixtureId(), _ => BackingProvider.CreateAsyncScope()).ServiceProvider;

        /// <summary>
        /// Gives the current fixture a new DI scope, so that its tests do not share the dependencies registered with "AddScoped" of earlier ones,
        /// and disposes the one it had.
        /// </summary>
        public static void NewScope()
        {
            var newScope = BackingProvider.CreateAsyncScope();
            Scopes.AddOrUpdate(CurrentFixtureId(), newScope, (_, oldScope) =>
            {
                oldScope.DisposeAsync().AsTask().Wait();
                return newScope;
            });
        }

        /// <summary>
        /// The id of the fixture the current test belongs to, or of the current suite (e.g. a set-up fixture) outside any fixture.
        /// </summary>
        public static string CurrentFixtureId() => CurrentFixture()?.Id ?? string.Empty;

        /// <summary>
        /// The fixture the current test belongs to, or the current suite (e.g. a set-up fixture) outside any fixture.
        /// </summary>
        public static ITest CurrentFixture()
        {
            ITest test = TestExecutionContext.CurrentContext.CurrentTest;
            for (var suite = test; suite != null; suite = suite.Parent)
            {
                if (suite is TestFixture)
                {
                    return suite;
                }
            }
            return test;
        }
    }
}
