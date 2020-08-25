using System;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.DonorImport.Test.Integration.DependencyInjection
{
    public static class DependencyInjection
    {
        private static IServiceProvider backingProvider;

        private static IServiceScope scope;

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
        /// Creates a new DI scope, to prevent all tests in the suite sharing any dependencies registered with "AddScoped".
        /// If a previous scope existed, disposes it before creating the new one.
        ///
        /// Usage:
        /// In the "SetUp" (or "OneTimeSetUp") of a test that needs to run on an independent scope, call this method. 
        /// </summary>
        internal static void NewScope()
        {
            scope?.Dispose();
            scope = BackingProvider.CreateScope();
        }

        internal static IServiceProvider Provider => scope.ServiceProvider;
    }
}