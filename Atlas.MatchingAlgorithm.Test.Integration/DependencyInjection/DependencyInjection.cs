using System;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection
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

        internal static void NewScope()
        {
            scope?.Dispose();
            scope = BackingProvider.CreateScope();
        }

        internal static IServiceProvider Provider => scope.ServiceProvider;
    }
}