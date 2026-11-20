using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection
{
    public static class DependencyInjection
    {
        private static IServiceProvider backingProvider;

        /// <summary>
        /// One DI scope per test fixture, so that fixtures running at the same time do not share scoped services (such as a DbContext),
        /// and a fixture that starts a new scope does not dispose the one another fixture is using.
        /// Code that runs outside any fixture (the set-up fixture) gets a scope of its own.
        /// </summary>
        private static readonly ConcurrentDictionary<string, IServiceScope> Scopes = new();

        internal static IServiceProvider BackingProvider
        {
            get
            {
                if (backingProvider == null)
                {
                    throw new Exception("Provider has not been set up");
                }
                return backingProvider;
            }
            set
            {
                backingProvider = value;
                NewScope();
            }
        }

        /// <summary>
        /// Creates a new DI scope for the current fixture, to prevent its tests sharing any dependencies registered with "AddScoped".
        /// If the fixture already had a scope, disposes it before creating the new one.
        ///
        /// Usage:
        /// In the "SetUp" (or "OneTimeSetUp") of a test that needs to run on an independent scope, call this method.
        /// </summary>
        internal static void NewScope()
        {
            var newScope = BackingProvider.CreateScope();
            Scopes.AddOrUpdate(CurrentFixtureId(), newScope, (_, oldScope) =>
            {
                oldScope.Dispose();
                return newScope;
            });
        }

        internal static IServiceProvider Provider => Scopes.GetOrAdd(CurrentFixtureId(), _ => BackingProvider.CreateScope()).ServiceProvider;

        /// <summary>
        /// The id of the fixture the current test belongs to, or of the current suite (e.g. the set-up fixture) outside any fixture.
        /// </summary>
        internal static string CurrentFixtureId() => CurrentFixture()?.Id ?? string.Empty;

        /// <summary>
        /// The fixture the current test belongs to, or the current suite (e.g. the set-up fixture) outside any fixture.
        /// </summary>
        internal static ITest CurrentFixture()
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
