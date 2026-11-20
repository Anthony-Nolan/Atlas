using System;
using Atlas.Common.Test.SharedTestHelpers;

namespace Atlas.DonorImport.Test.Integration.DependencyInjection
{
    /// <summary>
    /// One DI scope per test fixture, as fixtures run at the same time; a fixture that sets <see cref="BackingProvider"/> sets it for itself only.
    /// See <see cref="TestFixtureScopes"/>.
    /// </summary>
    public static class DependencyInjection
    {
        internal static IServiceProvider BackingProvider
        {
            get => TestFixtureScopes.BackingProvider;
            set => TestFixtureScopes.BackingProvider = value;
        }

        /// <summary>
        /// Creates a new DI scope for the current fixture, to prevent its tests sharing any dependencies registered with "AddScoped".
        ///
        /// Usage:
        /// In the "SetUp" (or "OneTimeSetUp") of a test that needs to run on an independent scope, call this method.
        /// </summary>
        internal static void NewScope() => TestFixtureScopes.NewScope();

        internal static IServiceProvider Provider => TestFixtureScopes.Provider;
    }
}
